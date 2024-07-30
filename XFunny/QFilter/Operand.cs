using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XFunny.QFilter
{
    public class Operand
    {
        protected internal object valor;
        protected internal string key;

        public string Left { get { return key; } set { key = value; } }
        public CSTyteOperator OperandType { get; set; }
        public object Right { get { return valor; } set { valor = value; } }

        public string Join() 
        {
            return string.Format("{0} = {1}", this.Left, this.Right);
        }
        /*
        public static bool operator  ==(string left, string right)
        {
            return false;
        }

        public static bool operator >=(Conditions left, Conditions right)
        {
            return false;
        }

        public static bool operator <=(Conditions left, Conditions right)
        {
            return false;
        }

        public static bool operator !=(Conditions left, Conditions right)
        {
            return false;
        }
        */        
    }
}
