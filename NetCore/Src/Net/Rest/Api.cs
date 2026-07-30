/*
    This file is part of the DECa (R) project.
    Copyright (c) 2026 Irene Solutions SL
    Authors: Irene Solutions SL.

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License version 3
    as published by the Free Software Foundation with the addition of the
    following permission added to Section 15 as permitted in Section 7(a):
    FOR ANY PART OF THE COVERED WORK IN WHICH THE COPYRIGHT IS OWNED BY
    IRENE SOLUTIONS SL. IRENE SOLUTIONS SL DISCLAIMS THE WARRANTY OF NON INFRINGEMENT
    OF THIRD PARTY RIGHTS
    
    This program is distributed in the hope that it will be useful, but
    WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
    or FITNESS FOR A PARTICULAR PURPOSE.
    See the GNU Affero General Public License for more details.
    You should have received a copy of the GNU Affero General Public License
    along with this program; if not, see http://www.gnu.org/licenses or write to
    the Free Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
    Boston, MA, 02110-1301 USA, or download the license from the following URL:
        http://www.irenesolutions.com/terms-of-use.pdf
    
    The interactive user interfaces in modified source and object code versions
    of this program must display Appropriate Legal Notices, as required under
    Section 5 of the GNU Affero General Public License.
    
    You can be released from the requirements of the license by purchasing
    a commercial license. Buying such a license is mandatory as soon as you
    develop commercial activities involving the DECa software without
    disclosing the source code of your own applications.
    These activities include: offering paid services to customers as an ASP,
    serving DECa services on the fly in a web application, 
    shipping DECa with a closed source product.
    
    For more information, please contact Irene Solutions SL. at this
    address: info@irenesolutions.com
 */

namespace DeCA.Net.Rest
{

    /// <summary>
    /// Representa el API REST de Irene Solutions para Verifactu.
    /// </summary>
    public class Api
    {

        #region Propiedades Privadas Estáticas


        /// <summary>
        /// Endpoint ct.
        /// </summary>
        internal static readonly string EndPointCt = "https://facturae.irenesolutions.com:8050/Kivu/Taxes/DECa/Ct/Create/Test/v17";

        #endregion

        #region Construtores de Instancia

        /// <summary>
        /// Constructor.
        /// </summary>
        public Api()
        {
        }

        #endregion

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Endpoint creación de factruas.
        /// </summary>
        public string EndPointCreate { get; set; }

        /// <summary>
        /// Endpoint anulación de factruas.
        /// </summary>
        public string EndPointCancel { get; set; }

        /// <summary>
        /// Endpoint consulta emisores.
        /// </summary>
        public string EndPointGetSellers { get; set; }

        /// <summary>
        /// Endpoint consulta registros envíados.
        /// </summary>
        public string EndPointGetRecords { get; set; }

        /// <summary>
        /// Endpoint consulta registros en DeCA.
        /// </summary>
        public string EndPointGetDECaInvoices { get; set; }

        /// <summary>
        /// Endpoint consulta envíos realizados.
        /// </summary>
        public string EndPointGetFilteredList { get; set; }

        /// <summary>
        /// Clave de acceso al API REST para DECa de
        /// Irene Solutions. Puede conseguir su clave en
        /// https://facturae.irenesolutions.com/deca/go
        /// </summary>
        public string ServiceKey { get; set; }

        #endregion

    }

}