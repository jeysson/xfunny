using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Reflection;
using System.ComponentModel;
using System.Data;
using XFunny.QFilter;
using XFunny.QAccess;

namespace XFunny.QConnecting
{
    /// <summary>
    /// Classe de gerenciamento de conexão do framework
    /// </summary>
    public sealed class QConnect : IDisposable
    {
        /// <summary>
        /// conector sql server
        /// </summary>
        private SqlConnection _Connect;

        /// <summary>
        /// Assembly base
        /// </summary>
        private Assembly _Project;

        /// <summary>
        /// String de conexão
        /// </summary>
        private string _ConnectionString;

        /// <summary>
        /// Objeto transação
        /// </summary>
        private SqlTransaction _Transaction;

        /// <summary>
        /// String de conexão
        /// </summary>
        public string ConnectionString { get { return _ConnectionString; } }

        /// <summary>
        /// Construtor
        /// </summary>
        public QConnect() 
        {            
            _Project = Assembly.GetEntryAssembly();
            //
            _Connect = new SqlConnection();                        
        }

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="ConnectionString">String de conexão</param>
        public QConnect(string pConnectionString)
        {
            try
            {
                _ConnectionString = pConnectionString;
                _Project = Assembly.GetEntryAssembly();
                //
                _Connect = new SqlConnection();
                _Connect.ConnectionString = _ConnectionString;
                _Connect.ConnectionString = pConnectionString.Replace(_Connect.Database, "master");
                _Connect.Open();
                // Cria a base de dados
                CreateDataBase();
                // Cria as tabelas
                TransformerObject();
            }
            catch (SqlException ex)
            {
                throw new ApplicationException(ex.Message);
            }
            finally
            {
                if (_Connect.State == System.Data.ConnectionState.Open)
                    _Connect.Close();
                _Connect.ConnectionString = _ConnectionString;
            }
        }

        /// <summary>
        /// Tranforma as classes em objetos relacionais
        /// </summary>
        internal void TransformerObject()
        {
            foreach (var classe in Assembly.GetEntryAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(QObjectBase))))            
                CreateTable(classe); 
            //Cria os relacionamentos entre os objetos
            foreach (var classe in Assembly.GetEntryAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(QObjectBase))))
            {
                // Monta os campos e domínios da tabela
                foreach (var proper in classe.GetProperties()
                    .Where(p => p.GetCustomAttributes(typeof(AssociationAttribute), false).Count() > 0 && p.PropertyType.IsSubclassOf(typeof(QObjectBase))))
                {                    
                    var customAttribute = Attribute.GetCustomAttribute(proper, typeof(AssociationAttribute)) as AssociationAttribute;
                    if (!ExistsConstraint("FK_" + customAttribute.Name))
                        CreateConstraint(classe.Name, proper.PropertyType.Name, "FK_" + customAttribute.Name, proper.Name, "OCod", customAttribute.Associate.Equals(AssociationAttribute.CSTypeAssociate.Composition));
                }                    
            }            
        }

        /// <summary>
        /// Destroi o objeto
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Seta nulo para os objetos istanciados
        /// </summary>
        /// <param name="disposing">verdadeiro para desstruir</param>
        internal void Dispose(bool disposing)
        {
            if (disposing)
                if (_Connect != null)
                {
                    _Connect.Dispose();
                    _Connect = null;
                }
        }

        /// <summary>
        /// Desconstrutor
        /// </summary>
        ~QConnect()
        {
            Dispose(false);
        }

        /// <summary>
        /// Abre uma conexão com a base de dados
        /// </summary>
        public void Open() 
        {
            try
            {  
                if(_Connect.State == ConnectionState.Closed)
                    _Connect.Open();
            }
            catch (SqlException ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }

        /// <summary>
        /// Abre uma transação com a base dados
        /// </summary>
        public void OpenTransaction() 
        {
            this._Transaction = this._Connect.BeginTransaction();
        }

        /// <summary>
        /// Efetiva as operações realizadas na base de dados
        /// </summary>
        public void CommitTransaction() 
        {
            this._Transaction.Commit();
        }

        /// <summary>
        /// Descarta um transação em uso
        /// </summary>
        public void DescartTransaction() 
        {
            this._Transaction.Dispose();
        }

        /// <summary>
        /// Desfaz as operações com o banco de dados 
        /// </summary>
        public void RollBackTransaction()
        {
            this._Transaction.Rollback();
        }

        /// <summary>
        /// Fecha a conexão com a base de dados 
        /// </summary>
        public void Close() 
        {
            _Connect.Close();
        }

        /// <summary>
        /// Verifica se existe uma baco de dados criado para solução
        /// </summary>
        /// <param name="pCommand">Executor de querys</param>
        /// <returns>verdadeiro quando existir</returns>
        private bool ExistsDataBase(SqlCommand pCommand)
        {
            bool exists; 
            pCommand.CommandText = string.Format("SELECT * FROM sys.databases WHERE name = '{0}'", _Project.GetName().Name);
            var dr = pCommand.ExecuteReader();
            exists = dr.HasRows;
            dr.Close();
            return exists;
        }

        /// <summary>
        /// Cria a base de dados com o mesmo nome do Projeto
        /// </summary>
        private void CreateDataBase() 
        {            
            using (SqlCommand command = _Connect.CreateCommand())
            {
                if (!ExistsDataBase(command))
                {
                    command.CommandText = string.Format("CREATE DATABASE {0}", _Project.GetName().Name);
                    command.ExecuteScalar();
                    // Cria tabela de controle
                    CreateTableControl(command);
                    // Atualiza tabela de controle
                    InsertTableControl(command);
                }
            }           
        }

        /// <summary>
        /// Cria a tabela de controle do framework
        /// </summary>
        /// <param name="pCommand">Executor de querys</param>
        private void CreateTableControl(SqlCommand pCommand) 
        { 
            StringBuilder sbQuery = new StringBuilder();
            sbQuery.AppendLine(string.Format("CREATE TABLE {0}.dbo.basecontrol", _Project.GetName().Name));
            sbQuery.AppendLine(" (");
            sbQuery.AppendLine(" name VARCHAR(50),");
            sbQuery.AppendLine(" completename VARCHAR(100),");
            sbQuery.AppendLine(" version varchar(15)");            
            sbQuery.AppendLine(" )");
            // Executa a query de criação da tabela
            pCommand.CommandText = sbQuery.ToString();
            pCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Cria a tabela de controle da aplicação
        /// </summary>
        /// <param name="pCommand">Executor de querys</param>
        public void CreateTable(Type pType)
        {
            try
            {
                // Cria uma objeto para executar a query
                using (var command = this._Connect.CreateCommand())
                {
                    if (!ExistsTable(command, pType.Name))
                    {
                        bool first = true;
                        // Executa a query de criação da tabela
                        StringBuilder sbQuery = new StringBuilder();
                        sbQuery.AppendLine(string.Format("CREATE TABLE {0}.dbo.{1}", _Project.GetName().Name, pType.Name));
                        sbQuery.AppendLine(" (");
                        // Monta os campo e domínios da tabela
                        foreach (var proper in pType.GetProperties())
                            if (proper.GetCustomAttributes(typeof(NonPersistentAttribute), false).Count() == 0 && 
                                !(proper.PropertyType.IsGenericType && proper.PropertyType.GetGenericTypeDefinition().Equals(typeof(QCollection<>))))
                            {
                                SqlDbType tipo;
                                if (proper.PropertyType.IsSubclassOf(typeof(QObjectBase)))
                                    tipo = GetSqlType(typeof(Guid));
                                else
                                    tipo = GetSqlType(proper.PropertyType);
                                //
                                if (first)
                                {
                                    first = false;
                                    if (tipo == SqlDbType.NVarChar)
                                        sbQuery.AppendLine(string.Format(" {0} {1}(255)", proper.Name, tipo));
                                    else
                                        if(tipo == SqlDbType.UniqueIdentifier)
                                            sbQuery.AppendLine(string.Format(" {0} {1} NOT NULL", proper.Name, tipo));
                                        else
                                            sbQuery.AppendLine(string.Format(" {0} {1}", proper.Name, tipo));
                                }
                                else 
                                {
                                    if (tipo == SqlDbType.NVarChar)
                                        sbQuery.AppendLine(string.Format(", {0} {1}(255)", proper.Name, GetSqlType(proper.PropertyType))); 
                                    else
                                        if (tipo == SqlDbType.UniqueIdentifier)
                                            sbQuery.AppendLine(string.Format(", {0} {1} NOT NULL", proper.Name, tipo));
                                        else
                                            sbQuery.AppendLine(string.Format(", {0} {1}", proper.Name, GetSqlType(proper.PropertyType))); 
                                }
                            }
                        //
                        sbQuery.AppendLine(" )");
                        //
                        command.CommandText = sbQuery.ToString();
                        command.ExecuteNonQuery();
                        if(!ExistsConstraint(string.Format("PK_{0}",pType.Name)))
                            //Cria a chave primária para o objeto
                            CreateConstraint(pType.Name, String.Format("PK_{0}", pType.Name), "OCod");
                    }  
                }
            }catch(SqlException ex)
            {
                throw new ApplicationException(string.Format("Error: {0}\n\t Description: {1}", ex.Message, ex.StackTrace));
            }            
        }

        /// <summary>
        /// Verifica se já existe uma constraint na base de dados
        /// </summary>
        /// <param name="p">Nome da constraint</param>
        /// <returns>Se existir a constraint retornará verdadeiro</returns>
        private bool ExistsConstraint(string pName)
        {
            bool exists;
            StringBuilder sbQuery = new StringBuilder();
            sbQuery.AppendLine("SELECT * ");
            sbQuery.AppendLine(string.Format("  FROM {0}.INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS", _Project.GetName().Name));
            sbQuery.AppendLine(string.Format(" WHERE CONSTRAINT_NAME ='{0}'", pName));
            //
            var command = this._Connect.CreateCommand();
            command.CommandText = sbQuery.ToString();
            var dr = command.ExecuteReader();
            exists = dr.HasRows;
            dr.Close();
            return exists;
        }

        /// <summary>
        /// verifica se a tabela já existe na base de dados
        /// </summary>
        /// <param name="pName">Nome da tabela</param>
        /// <returns>verdadeiro se existir</returns>
        private bool ExistsTable(SqlCommand pCommand, string pName) 
        {
            bool exists;
            StringBuilder sbQuery = new StringBuilder();
            sbQuery.AppendLine("SELECT * ");
            sbQuery.AppendLine(string.Format("  FROM {0}.INFORMATION_SCHEMA.TABLES", _Project.GetName().Name));
            sbQuery.AppendLine(string.Format(" WHERE TABLE_CATALOG = '{0}'", _Project.GetName().Name));
            sbQuery.AppendLine(string.Format("   AND TABLE_NAME = '{0}'", pName));
            sbQuery.AppendLine("   AND TABLE_TYPE = 'BASE TABLE'");
            pCommand.CommandText = sbQuery.ToString();
            var dr = pCommand.ExecuteReader();
            exists = dr.HasRows;
            dr.Close();
            return exists;
        }

        /// <summary>
        /// Cria a chave primaria da tabela
        /// </summary>
        /// <param name="pNameTable">Nome da tabela</param>
        /// <param name="pNameConstraint">Nome da Chave Primária</param>
        /// <param name="pKey">Nome do campo chave primária</param>
        private void CreateConstraint(string pNameTable, string pNameConstraint, string pKey)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format(" ALTER TABLE {0}.dbo.{1}", _Project.GetName().Name, pNameTable));
            sb.AppendLine(string.Format(" ADD CONSTRAINT {0}", pNameConstraint));            
            sb.Append(string.Format(" PRIMARY KEY ({0})", pKey));            
            //
            using (var command = this._Connect.CreateCommand())
            {
                command.CommandText = sb.ToString();
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Cria as cronstraints na base de dados para cada tabela
        /// </summary>
        /// <param name="pNameTable">Nome da tabela da constraint</param>
        /// <param name="pReferencesTable">Nome da tabela referenciada da constraint para foreing key</param>
        /// <param name="pNameConstraint">Nome da constrait</param>       
        /// <param name="pKey">Campos que serão chave</param>
        /// <param name="pReferenceKey">Campos refenciados para foreing key</param>
        /// <param name="pCascade">tipo de atualização</param>
        private void CreateConstraint(string pNameTable, string pReferencesTable, string pNameConstraint, string pKey, string pReferenceKey, bool pCascade = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format(" ALTER TABLE {0}.dbo.{1}", _Project.GetName().Name, pNameTable));
            sb.AppendLine(string.Format(" ADD CONSTRAINT {0}", pNameConstraint));            
            sb.AppendLine(string.Format(" FOREIGN KEY ({0})", pKey));
            sb.AppendLine(string.Format(" REFERENCES {0}.dbo.{1} ({2}) ", _Project.GetName().Name, pReferencesTable, pReferenceKey));           
            //
            if (pCascade)                
                sb.AppendLine("ON DELETE CASCADE ON UPDATE CASCADE");                               
            //            
            using (var command = this._Connect.CreateCommand())
            {
                command.CommandText = sb.ToString();
                command.ExecuteNonQuery();
            }

        }

        /// <summary>
        /// Insere os primeiros valores da tabela de controle do framework 
        /// </summary>
        /// <param name="pCommand">Executor de querys</param>
        private void InsertTableControl(SqlCommand pCommand)
        {
            StringBuilder sbQuery = new StringBuilder();
            sbQuery.AppendLine(string.Format("INSERT INTO {0}.dbo.basecontrol", _Project.GetName().Name));
            sbQuery.AppendLine(" ( name, completename, version)");
            sbQuery.AppendLine(string.Format(" SELECT '{0}'", _Project.GetName().Name));
            sbQuery.AppendLine(string.Format(" , '{0}'", _Project.GetName().FullName));
            sbQuery.AppendLine(string.Format(" , '{0}'", _Project.GetName().Version));            
            //
            pCommand.CommandText = sbQuery.ToString();
            pCommand.ExecuteScalar();
        }

        /// <summary>
        /// Armazena os valores do objeto
        /// </summary>        
        internal void Insert(QObjectBase pObjectBase)
        {
            bool first = true;            
            StringBuilder sbQuery = new StringBuilder();
            StringBuilder sbValues = new StringBuilder();
            sbQuery.AppendLine(string.Format("INSERT INTO {0}.dbo.{1} ", _Project.GetName().Name, pObjectBase.GetType().Name));
            sbQuery.AppendLine(" (");
            sbValues.Append(" VALUES (");
            foreach (PropertyInfo proper in pObjectBase.GetType().GetProperties())
            {
                if (proper.GetCustomAttributes(typeof(NonPersistentAttribute), false).Count() == 0 &&
                    !(proper.PropertyType.IsGenericType && proper.PropertyType.GetGenericTypeDefinition().Equals(typeof(QCollection<>))))
                {
                    if (first)
                    {                        
                        sbQuery.AppendLine(proper.Name);
                        sbValues.AppendLine();
                        if(proper.PropertyType == typeof(string) || proper.PropertyType == typeof(char) || proper.PropertyType == typeof(bool))
                            sbValues.Append(String.Format("'{0}'", proper.GetValue(pObjectBase, null)));
                        else if (proper.PropertyType == typeof(Guid) || proper.PropertyType.IsSubclassOf(typeof(QObjectBase)))                        
                            sbValues.Append(String.Format("CONVERT( uniqueidentifier, '{0}')", proper.GetValue(pObjectBase, null)));                        
                        else
                            sbValues.Append(proper.GetValue(pObjectBase, null));
                        first = false;
                    }
                    else 
                    {
                        sbQuery.Append(", ");
                        sbQuery.AppendLine(proper.Name);
                        if (proper.PropertyType == typeof(string) || proper.PropertyType == typeof(char) || proper.PropertyType == typeof(bool))
                        {
                            sbValues.Append(", ");
                            sbValues.Append(String.Format("'{0}'", proper.GetValue(pObjectBase, null)));
                        }
                        else if (proper.PropertyType == typeof(Guid) || proper.PropertyType.IsSubclassOf(typeof(QObjectBase)))
                        {
                            sbValues.Append(", ");
                            sbValues.Append(String.Format("CONVERT( uniqueidentifier, '{0}')", proper.GetValue(pObjectBase, null)));
                        }
                        else
                        {
                            sbValues.Append(", ");
                            sbValues.Append(proper.GetValue(pObjectBase, null));
                        }                        
                    }
                }
            }
            sbQuery.AppendLine(" )");
            sbValues.AppendLine(" )");
            sbQuery.AppendLine(sbValues.ToString()); 
            //
            using (var command = _Connect.CreateCommand())
            {
                command.CommandText = sbQuery.ToString();
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Atualiza os valores do objeto 
        /// </summary>        
        internal void Update(QObjectBase pObjectBase)
        {
            bool first = true;
            StringBuilder sbQuery = new StringBuilder();            
            sbQuery.AppendLine(string.Format("UPDATE {0}.dbo.{1} ", _Project.GetName().Name, pObjectBase.GetType().Name));
            sbQuery.AppendLine(" SET ");
            
            foreach (PropertyInfo proper in pObjectBase.GetType().GetProperties())
            {
                if (proper.GetCustomAttributes(typeof(NonPersistentAttribute), false).Count() == 0 &&
                    !proper.PropertyType.Equals(typeof(Guid)))
                {
                    if (first)
                    {
                        sbQuery.AppendLine(proper.Name);
                        sbQuery.Append(" = ");
                        if (proper.PropertyType == typeof(string) || proper.PropertyType == typeof(char))
                            sbQuery.Append(String.Format("'{0}'", proper.GetValue(pObjectBase, null)));
                        else
                            sbQuery.Append(proper.GetValue(pObjectBase, null));
                        first = false;
                    }
                    else
                    {
                        sbQuery.Append(",");
                        sbQuery.Append(proper.Name);
                        sbQuery.Append(" = ");
                        if (proper.PropertyType == typeof(string) || proper.PropertyType == typeof(char))
                            sbQuery.Append(String.Format("'{0}'", proper.GetValue(pObjectBase, null)));
                        else
                            sbQuery.Append(proper.GetValue(pObjectBase, null));
                    }
                    sbQuery.AppendLine();                   
                }
            }
            sbQuery.AppendLine(string.Format(" WHERE OCOD = '{0}'", pObjectBase.OCod));
            // Executa a query
            using (var command = _Connect.CreateCommand())
            {
                command.CommandText = sbQuery.ToString();
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Exclui a base de dados
        /// </summary>
        public void DropDataBase() 
        {
            StringBuilder sbQuery = new StringBuilder();
            sbQuery.AppendLine(string.Format("DROP DATABASE {0}", _Project.GetName().Name));
            // Cria uma objeto para execcutar a query
            using (var command = this._Connect.CreateCommand())
            {
                command.CommandText = sbQuery.ToString();
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Pega o domínio do campo na base de dados
        /// </summary>
        /// <param name="pValue">tipo a ser convertido System.Type</param>
        /// <returns>valor do domínio </returns>
        private SqlDbType GetSqlType(Type pValue )
        {
            // Cria um parametro
            SqlParameter param = new SqlParameter();
            // cria um tipo converter
            TypeConverter tc = TypeDescriptor.GetConverter(param.DbType);
            // Veririca se é possível converter
            if (tc.CanConvertFrom(pValue) )
                param.DbType = (DbType)tc.ConvertFrom(pValue.Name);
            else{
                try
                {
                    // Força a conversão
                    param.DbType = (DbType)tc.ConvertFrom(pValue.Name);
                }
                catch { }                    
            }        

            return param.SqlDbType;
        }

        /// <summary>
        /// Define a string de conexão com a base de dados
        /// </summary>
        /// <param name="pConnectionString">string de conexão</param>
        public void SetConnectionString(string pConnectionString)
        {
            if (string.IsNullOrEmpty(_ConnectionString))
                _ConnectionString = pConnectionString;
        
        }

        internal DataTable ExcuteQuery(string p)
        {
            try
            {
                this.Open();
                using (var command = this._Connect.CreateCommand())
                {
                    command.CommandText = p;
                    var dr = command.ExecuteReader();
                    DataTable dtb = new DataTable();
                    dtb.Load(dr);
                    dr.Close();
                    return dtb;
                }
            }
            finally { this.Close(); }
        }

        enum CSTypeConstraint
        {
            Primary = 0,
            Foreign = 1
        }

        /// <summary>
        /// Remove os objetos pesistidos
        /// </summary>
        /// <param name="qObjectBase">objeto removido</param>
        internal void Delete(QObjectBase pObjectBase)
        {
            StringBuilder sbQuery = new StringBuilder();
            sbQuery.AppendLine(string.Format("DELETE FROM {0}.dbo.{1}", _Project.GetName().Name, pObjectBase.GetType().Name));
            sbQuery.AppendLine(string.Format("WHERE OCod = '{0}'", pObjectBase.OCod));
            // Cria uma objeto para execcutar a query
            using (var command = this._Connect.CreateCommand())
            {
                command.CommandText = sbQuery.ToString();
                command.ExecuteNonQuery();
            }
        }
    }
}
