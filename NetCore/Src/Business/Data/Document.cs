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

using DeCA.Net.Rest.Json;
using DeCA.Net.Rest.Json.Kivu;
using System;
using System.Collections.Generic;

namespace DeCA.Business.Data
{

    /// <summary>
    /// Representa un Documento Electrónico de Control Administrativo
    /// para el transporte público de mercancías por carretera.
    /// </summary>
    public class Document : JsonSerializableKivu
    {

        #region Propiedades Públicas de Instancia

        /// <summary>
        /// Identificador del interlocutor que representa a la empresa
        /// propietaria del documento en el sistema de origen.
        /// </summary>
        public string OwnerPartyID { get; set; }

        /// <summary>
        /// Identificador único del documento electrónico de control.
        /// </summary>
        public string DeCAID { get; set; }

        /// <summary>
        /// Número o referencia visible del documento.
        /// </summary>
        public string DocumentNumber { get; set; }

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
        public DateTime? CreationDateTime { get; set; }

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
        /// Año de emisión del documento.
        /// </summary>
        public string IssueYear => $"{IssueDateTime?.Year}";

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

        /// <summary>
        /// Interlocutores de negocio, establecimientos y lugares que intervienen
        /// en el transporte.
        ///
        /// El tipo de participación de cada interlocutor se identifica mediante
        /// la propiedad <see cref="Party.PartyRole"/>.
        /// </summary>
        public List<Party> Parties { get; set; }

        /// <summary>
        /// Descripción general de la naturaleza de la mercancía.
        /// </summary>
        public string GoodsDescription { get; set; }

        /// <summary>
        /// Cantidad total de la mercancía.
        /// </summary>
        public decimal GoodsQuantity { get; set; }

        /// <summary>
        /// Código de unidad de medida de la cantidad de mercancía.
        /// Ejemplos: KGM, TNE, LTR o MTQ.
        /// </summary>
        public string GoodsQuantityUnitCode { get; set; }

        /// <summary>
        /// Peso bruto total de la mercancía expresado en kilogramos.
        /// </summary>
        public decimal? GrossWeight { get; set; }

        /// <summary>
        /// Indica si la mercancía está sometida a la normativa ADR.
        /// </summary>
        public bool IsDangerousGoods { get; set; }

        /// <summary>
        /// Matrícula del vehículo tractor.
        /// </summary>
        public string TractorRegistrationNumber { get; set; }

        /// <summary>
        /// Matrícula del remolque o semirremolque.
        /// </summary>
        public string TrailerRegistrationNumber { get; set; }

        /// <summary>
        /// Matrícula de un segundo remolque, cuando corresponda.
        /// </summary>
        public string SecondTrailerRegistrationNumber { get; set; }

        /// <summary>
        /// Número de la autorización especial de circulación,
        /// cuando resulte necesaria.
        /// </summary>
        public string SpecialCirculationAuthorizationNumber { get; set; }

        /// <summary>
        /// Observaciones, reservas o indicaciones adicionales.
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// Motivo de la última modificación del documento.
        /// </summary>
        public string ModificationReason { get; set; }

        /// <summary>
        /// Identificador del documento anterior cuando este documento
        /// constituye una nueva versión o sustitución.
        /// </summary>
        public string PreviousDeCAID { get; set; }

        /// <summary>
        /// Nombre del fichero PDF generado.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Tamaño del fichero PDF en bytes.
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// Hash SHA-256 del fichero PDF.
        /// </summary>
        public string FileHash { get; set; }

        /// <summary>
        /// URL HTTPS única que permite descargar directamente el PDF.
        /// </summary>
        public string DownloadURL { get; set; }

        /// <summary>
        /// Contenido incorporado al código QR.
        /// Normalmente coincidirá con la URL de descarga.
        /// </summary>
        public string QRCodeValue { get; set; }

        /// <summary>
        /// Fecha y hora desde la que el documento está disponible
        /// mediante la URL de descarga.
        /// </summary>
        public DateTime? DownloadAvailableFromDateTime { get; set; }

        /// <summary>
        /// Fecha y hora hasta la que el documento estará disponible
        /// mediante la URL de descarga.
        /// </summary>
        public DateTime? DownloadAvailableUntilDateTime { get; set; }

        /// <summary>
        /// Nombre o identificador del sistema de origen de los datos.
        /// Ejemplos: SAP, Wefinz o integración API.
        /// </summary>
        public string SourceSystem { get; set; }

        /// <summary>
        /// Identificador de la expedición, entrega o transporte
        /// en el sistema de origen.
        /// </summary>
        public string SourceDocumentID { get; set; }

        /// <summary>
        /// Clave técnica o clave primaria del documento en el sistema de origen.
        /// </summary>
        public string SourceDocumentKey { get; set; }

        /// <summary>
        /// Usuario o proceso que creó el documento.
        /// </summary>
        public string CreatedBy { get; set; }

        /// <summary>
        /// Usuario o proceso que realizó la última modificación.
        /// </summary>
        public string ModifiedBy { get; set; }

        /// <summary>
        /// Datos binarios de la plantilla PDF utilizada para generar el PDF definitivo del documento DeCA.
        /// </summary>
        [Json(JsonIgnore = true)]
        public byte[] SourcePdfTemplate { get; set; }

        /// <summary>
        /// Nombre archivo de la plantilla PDF utilizada para generar el PDF definitivo del documento DeCA.
        /// </summary>
        [Json(JsonIgnore = true)]
        public string SourcePdfFileName { get; set; }

        /// <summary>
        /// Contenido incorporado al código QR.
        /// Normalmente coincidirá con la URL de descarga.
        /// </summary>
        [Json(JsonIgnore = true)]
        public byte[] QRCode { get; set; }

        #endregion

        #region Constructores Públicos de Instancia

        /// <summary>
        /// Inicializa una nueva instancia de la clase <see cref="DeCA"/>.
        /// </summary>
        public Document()
        {
            Parties = new List<Party>();
        }

        #endregion

        #region Métodos Públicos de Instancia

        /// <summary>
        /// Obtiene la representación textual del documento electrónico
        /// de control.
        /// </summary>
        /// <returns>
        /// Número del documento y descripción de la mercancía.
        /// </returns>
        public override string ToString()
        {
            return $"{DocumentNumber}, {GoodsDescription}";
        }

        #endregion

    }
}
