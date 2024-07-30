using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XFunny.QAccess
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AssociationAttribute: Attribute
    {
        /// <summary>
        /// Nome da Associação
        /// </summary>
        private string _Name;

        /// <summary>
        /// Tipo de Associação
        /// </summary>
        private CSTypeAssociate _Associate;

        /// <summary>
        /// Nome da Associação
        /// </summary>
        public string Name
        {
            get { return _Name; }           
        }

        /// <summary>
        /// Tipo de Associação
        /// </summary>
        public CSTypeAssociate Associate 
        {
            get { return _Associate; }            
        }

        public AssociationAttribute(string pName, CSTypeAssociate type) 
        {
            this._Associate = type;
            this._Name = pName;
        }

        public enum CSTypeAssociate
        {
            Aggregation = 0
                , 
            Composition = 1
        }
    }

    public class ConstraintAttribute : Attribute 
    {
        /// <summary>
        /// Tipo de chave
        /// </summary>
        private CSTypeConstraint _TypeConstraint;

        /// <summary>
        /// Tipo de chave
        /// </summary>
        public CSTypeConstraint TypeConstraint 
        { 
            get { return _TypeConstraint; }            
        }

        public ConstraintAttribute(CSTypeConstraint pType) 
        {
            this._TypeConstraint = pType;
        }

        /// <summary>
        /// Tipos de chaves
        /// </summary>
        public enum CSTypeConstraint 
        {
            Primary = 0
                ,
            Foreign = 1
        }
    }
}
