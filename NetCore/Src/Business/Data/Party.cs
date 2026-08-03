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

using DeCA.Net.Rest.Json.Kivu;

namespace DeCA.Business.Data
{
    /// <summary>
    /// Representa un interlocutor en un proceso de negocio.
    /// principales.
    /// </summary>
    public class Party : JsonSerializableKivu
    {

        #region Public Properties

        /// <summary>
        /// <para>'CC': Cargador contractual.</para>
        /// <para>'TE': Transportista efectivo.</para>
        /// <para>'EX': Expedidor o cargador efectivo.</para>
        /// <para>'DS': Destinatario de la mercancía.</para>
        /// <para>'OR': Lugar, establecimiento o interlocutor de origen.</para>
        /// <para>'DE': Lugar, establecimiento o interlocutor de destino.</para>
        /// <para>'OT': Otros.</para>
        /// </summary>
        public string PartyRole { get; set; }

        /// <summary>
        /// Identificador del interlocutor de negocio.
        /// </summary>
        public string PartyID { get; set; }

        /// <summary>
        /// Nombre asignado al interlocutor.
        /// </summary>        
        public string FullName { get; set; }

        /// <summary>
        /// Código de identificación fiscal.
        /// </summary>        
        public string TaxID { get; set; }

        /// <summary>
        /// Dirección.
        /// </summary>        
        public string Address { get; set; }

        /// <summary>
        /// Población.
        /// </summary>        
        public string City { get; set; }

        /// <summary>
        /// Código postal.
        /// </summary>        
        public string PostalCode { get; set; }

        /// <summary>
        /// Código región: Ej. provincia.
        /// </summary>        
        public string Region { get; set; }

        /// <summary>
        /// Código país ISO-3166 (EJ. ES).
        /// </summary>        
        public string CountryID { get; set; }

        /// <summary>
        /// Dirección de correo principal.
        /// </summary>
        public string Mail { get; set; }

        /// <summary>
        /// Número de teléfono movil.
        /// </summary>
        public string Mobile { get; set; }

        /// <summary>
        /// Número de teléfono fijo.
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// Identificador del estado del documento.
        /// </summary>        
        public string Status { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Representacion textual del interlocutor de negocio.
        /// </summary>
        /// <returns>Representacion textual del interlocutor de negocio.</returns>
        public override string ToString()
        {
            return $"{FullName}, {TaxID}";
        }

        #endregion

    }

}
