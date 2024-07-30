using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Linq.Expressions;
using System.Data.Linq;
using System.Threading.Tasks;
using System.Data;
using XFunny.QAccess;
using XFunny.QConnecting;

namespace XFunny.QFilter
{    
    /// <summary>
    /// Classe coleção para armazenamento em memória de dados
    /// </summary>
    /// <typeparam name="T">Classe base</typeparam>    
    public class QCollection<T> where T  : class, new()

    {
        /// <summary>
        /// Lista com os objetos adicionados a coleção
        /// </summary>
        protected internal List<T> collection;
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pConnect"></param>
        /// <param name="pConditions"></param>
        public QCollection(QConnect pConnect, Conditions pConditions)
        {            
            var t = Task<string>.Factory.StartNew(() =>
            {
                return pConditions.GetQuery<T>();
            });
            t.Wait();
            ConvetDataTable(pConnect.ExcuteQuery(t.Result));
        }

        private void ConvetDataTable(System.Data.DataTable dataTable)
        {
            collection = new List<T>();
            
            foreach (DataRow row in dataTable.Rows)
            {
                T obj = new T();
                foreach (var proper in obj.GetType().GetProperties())
                {
                    if (proper.GetCustomAttributes(typeof(NonPersistentAttribute), false).Count() == 0)
                    {

                        proper.SetValue(obj, row[proper.Name], null);
                    }
                }

                collection.Add(obj);
            }            
        }

        /// <summary>
        /// Total de itens na lista
        /// </summary>
        public int Count
        {
            get { return collection.Count; }
        }

        /// <summary>
        /// Retona o item na posição definida
        /// </summary>
        /// <param name="index">Índice na lista</param>
        /// <returns>Retorna o objeto no índice na lista</returns>
        public T this[int index]
        {
            get
            {
                return this.collection[index];
            }

            set { this.collection[index] = value; }
        }

        /// <summary>
        /// adiciona um item na coleção
        /// </summary>
        /// <param name="pItem">Novo item da coleção</param>
        public void Add(T pItem)
        {
            this.collection.Add(pItem);
        }

        /// <summary>
        /// Lima a coleção
        /// </summary>
        public void Clear()
        {
            this.collection.Clear();
        }

        /// <summary>
        /// Verifica se contém o item na coleção
        /// </summary>
        /// <param name="pItem">Item a da coleção</param>
        /// <returns>verdadeiro quando um item é encontrado na coleção</returns>
        public bool Contains(T pItem)
        {
            return this.collection.Contains(pItem);
        }
        
        /// <summary>
        /// Remove o item da coleção
        /// </summary>
        /// <param name="item">Item a ser removido</param>
        /// <returns>Verdadeiro quando removido</returns>
        public bool Remove(T item)
        {
            return this.collection.Remove(item);
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerator<T> GetEnumerator()
        {
            yield return new T();  
        }
    }
}
