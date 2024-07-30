using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using XFunny.QFilter;
using System.Collections;
using XFunny.QConnecting;

namespace XFunny.QAccess
{
    // Summary:
    //     Provides the abstract (MustInherit in Visual Basic) base class for criteria
    //     operators.
    // summary:
    //      Classe base para objetos que representam a estrutura de dados    
    public abstract class QObjectBase : IObject, IDisposable
    {
        /// <summary>
        /// Objeto de connexão com a base de dados
        /// </summary>
        private QConnect _Connection;

        /// <summary>
        /// Identificador do objeto
        /// </summary>
        private Guid _OCod;

        /// <summary>
        /// Conexão com a base de dados
        /// </summary>
        [NonPersistent]
        public QConnect Connection { get { return _Connection; } }

        /// <summary>
        /// Identificador do objeto
        /// </summary>
        [Constraint(ConstraintAttribute.CSTypeConstraint.Primary)]
        public Guid OCod 
        { 
            get { return _OCod; }
            set { _OCod = value; }
        }

        /// <summary>
        /// Construtor
        /// </summary>
        public QObjectBase()
        {
            if (InitConnection.Connect == null)
            {
                InitConnection.CreateConnection();
                _Connection = InitConnection.Connect;                
            }
            else _Connection = InitConnection.Connect;
        }

        /// <summary>
        /// Construtor
        /// </summary>
        /// <param name="pConnect">Conexão</param>
        public QObjectBase(QConnect pConnect)
        {
            _Connection = pConnect;
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
        /// Define valores nulos para o objeto
        /// </summary>
        /// <param name="disposing">descatar</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                if (Connection != null)
                {
                    Connection.Dispose();
                    _Connection = null;
                }
        }

        /// <summary>
        /// Desconstrutor
        /// </summary>
        ~QObjectBase()
        {
            Dispose(false);
        }
        
        public virtual void OnSaving()
        {
            
        }

        public virtual void OnSalved()
        {
            
        }

        public virtual void OnDeleting()
        {
            
        }

        public virtual void OnDeleted()
        {
            
        }

        /// <summary>
        /// Salva o objeto
        /// </summary>
        public void Save()
        {
            try
            {
                _Connection.Open();

                OnSaving();

                if (_OCod.Equals(Guid.Empty))
                {
                    _OCod = Guid.NewGuid();
                    
                    this._Connection.Insert(this);
                    //salva os objetos da coleção
                    foreach (PropertyInfo proper in this.GetType().GetProperties())
                    {
                        if (proper.PropertyType.IsGenericType && proper.PropertyType.GetGenericTypeDefinition().Equals(typeof(QCollection<>)))
                        {
                            IEnumerable coll = (IEnumerable)proper.GetValue(this, null);
                            foreach (var item in coll)                            
                                ((QObjectBase)item).Save();                                                       
                        }                            
                    }
                    foreach (PropertyInfo proper in this.GetType().GetProperties().Where(p => p.PropertyType == typeof(XFunny.QFilter.QCollection<>)))
                    {
                        this.Connection.Insert(proper.GetValue(this, null) as QObjectBase);
                    }
                }
                else this._Connection.Update(this);

                OnSalved();
            }
            finally { _Connection.Close(); }
        }

        /// <summary>
        /// Remove o objeto da base de dados
        /// </summary>
        public void Delete() 
        {
            _Connection.Open();
            OnDeleting();
            _Connection.Delete(this);
            OnDeleted();
        }

        /// <summary>
        /// Retorna o identificador do objeto
        /// </summary>
        /// <returns>Código do objeto</returns>
        public override string ToString()
        {
            return OCod.ToString();
        }
    }
}
