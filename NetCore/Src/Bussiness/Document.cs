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
    serving DECa XML data on the fly in a web application, shipping DECa
    with a closed source product.
    
    For more information, please contact Irene Solutions SL. at this
    address: info@irenesolutions.com
 */

using DECa.Net.Rest.Json.Kivu;

namespace DECa.Business
{

    /// <summary>
    /// Representa un documento en el sistema DECa.
    /// </summary>
    public class Document : JsonSerializableKivu
    {

        #region Variables Privadas de Instancia

        /// <summary>
        /// Suma de las bases imponibles.
        /// </summary>   
        decimal _NetAmount;


        #endregion

        #region Construtores de Instancia

        /// <summary>
        /// Constructor.
        /// </summary>
        public Document() 
        {
        }

        #endregion


        #region Propiedades Públicas de Instancia

        /// <summary>
        /// <para> Identificador único del documento electrónico de control.</para>
        /// </summary>
        public string DeCAID { get; set; }

        /// <summary>
        /// Número de versión del documento.
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// Identificador del estado del documento.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Fecha y hora de creación del documento.
        /// </summary>
        public DateTime CreationDateTime { get; set; }

        /// <summary>
        /// Fecha y hora de la última modificación del documento.
        /// </summary>
        public DateTime? ModificationDateTime { get; set; }

        /// <summary>
        /// Fecha y hora de emisión definitiva del documento.
        /// Debe ser anterior al inicio efectivo del transporte.
        /// </summary>
        public DateTime? IssueDateTime { get; set; }

        /// <summary>
        /// Fecha prevista o efectiva del transporte.
        /// </summary>
        public DateTime TransportDate { get; set; }

        /// <summary>
        /// Fecha y hora de inicio efectivo del transporte.
        /// </summary>
        public DateTime? TransportStartDateTime { get; set; }

        /// <summary>
        /// Fecha y hora de finalización del transporte.
        /// </summary>
        public DateTime? TransportEndDateTime { get; set; }

        #endregion     

        #region Métodos Públicos de Instancia      

        /// <summary>
        /// Representación textual de la instancia.
        /// </summary>
        /// <returns> Representación textual de la instancia.</returns>
        public override string ToString()
        {

            return $"{DeCAID}";

        }

        #endregion

    }

}
