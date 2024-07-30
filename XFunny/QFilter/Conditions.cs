using System;
using System.Collections.Generic;
using System.Linq;

namespace XFunny.QFilter
{
    /// <summary>
    /// Classe que define as condições para busca
    /// </summary>
    public class Conditions: ICloneable
    {
        /// <summary>
        /// Campos do objeto
        /// </summary>
        protected internal List<Operand> _Fields;

        /// <summary>
        /// Construtor
        /// </summary>
        public Conditions() 
        {
            this._Fields = new List<Operand>();
        }

        public string GetQuery()
        {
            bool first = true;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("SELECT * FROM {0} WHERE ");
            foreach (var item in this._Fields)
            {
                if (!first)
                    sb.AppendLine("AND");

                sb.AppendLine(item.Join());
            }
            return sb.ToString();
        
        }

        /// <summary>
        /// Valores de condição da consulta
        /// </summary>
        /// <param name="pCondition">Condição de consulta</param>
        /// <param name="argsobj">Valores</param>
        /// <returns>Condição</returns>
        public static Conditions Values(string pCondition, params object[] argsobj) 
        {
            Conditions c = new Conditions();
            try
            {                
                var p = pCondition.Split(' ');
                
                for (int index = 0; index < p.Length; index = index + 3)
                {
                    var op = new Operand();
                    op.Left = p[index];
                    //
                    if (p[index + 1].Equals("="))
                        op.OperandType = CSTyteOperator.Equals;
                    else
                        if (p[index + 1].Equals("!="))
                            op.OperandType = CSTyteOperator.NotEquals;
                        else
                            if (p[index + 1].Equals(">"))
                                op.OperandType = CSTyteOperator.Greater;
                            else
                                if (p[index + 1].Equals(">="))
                                    op.OperandType = CSTyteOperator.GreaterEquals;
                                else
                                    if (p[index + 1].Equals("<"))
                                        op.OperandType = CSTyteOperator.Less;
                                    else
                                        if (p[index + 1].Equals("<="))
                                            op.OperandType = CSTyteOperator.LessEquals;
                    //
                    if (p[index + 2].Equals("?"))
                        op.Right = argsobj[index];
                    else
                        op.Right = p[index + 2];
                    c._Fields.Add(op);
                }
            }
            catch 
            {
                throw new ApplicationException("Campos da condição inválido!");
            }
            return c;
        }

        public static implicit operator Conditions(int p)
        {
            return new Conditions();
        }

        public object Clone()
        {
            throw new NotImplementedException();
        }


        internal string GetQuery<T>()
        {
            bool first = true;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(string.Format("SELECT * FROM {0} WHERE ", typeof(T).Name));
            foreach (var item in this._Fields)
            {
                if (!first)
                    sb.AppendLine("AND");

                sb.AppendLine(item.Join());
            }
            return sb.ToString();
        }
    }
}
