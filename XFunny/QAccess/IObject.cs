using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XFunny.QAccess
{
    /// <summary>
    /// Especifica ações do objeto quando impementado pela classe
    /// </summary>
    interface IObject
    {
        /// <summary>
        /// Quando impementado pela classe especifica o estado do objeto antes de ser salvo
        /// </summary>
        void OnSaving();

        /// <summary>
        /// Quando implementado pela classe especifica o estado do objeto após ser salvo
        /// </summary>
        void OnSalved();

        /// <summary>
        /// Quando implementado pela classe especifica o estado do objeto antes de ser deletado
        /// </summary>
        void OnDeleting();

        /// <summary>
        /// Quando implementado pela classe especifica o estado do objeto após ser deletado
        /// </summary>
        void OnDeleted();
    }
}
