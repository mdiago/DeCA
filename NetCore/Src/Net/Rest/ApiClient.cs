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

using System.Dynamic;
using System.Net;
using System.Text;
using DeCA.Common;
using DeCA.Config;
using DeCA.Net.Rest.Json.Kivu;
using DeCA.Net.Rest.Json.Parser;
using DeCA.Net.Rest.List;
using DeCA.Business.Data;
using System;


namespace DeCA.Net.Rest
{

    /// <summary>
    /// Representa un cliente API REST de Irene Solutions para Verifactu.
    /// </summary>
    public static class ApiClient
    {

        #region Variables Privadas Estáticas

        static Settings _Settings;

        #endregion

        #region Construtores Estáticos

        /// <summary>
        /// Constructor.
        /// </summary>
        static ApiClient() 
        {

            _Settings = Settings.Current;

        }

        #endregion

        #region Métodos Privados Estáticos

        /// <summary>
        /// Realiza una llamada al API y recupera el 
        /// resultado.
        /// </summary>
        /// <param name="input">Entrada para realizar la llamada.</param>
        /// <param name="url">Endpoint de la llamada.</param>
        /// <returns>Resultado llamada API.</returns>
        public static ExpandoObject Post(JsonSerializableKivu input, string url)
        {

            byte[] buff = null;

            input.ServiceKey = Settings.Current.Api.ServiceKey;

            using (WebClient wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                buff = wc.UploadData(url, Encoding.UTF8.GetBytes(input.ToJson()));
            }

            var json = Encoding.UTF8.GetString(buff);
            var jsonParser = new JsonParser(json);

            return jsonParser.GetResult();

        }

        #endregion

        #region Propiedades Públicas Estáticas

        /// <summary>
        /// Datos API cargados de configuración.
        /// </summary>
        public static Api Api 
        { 
            get 
            { 
                return _Settings.Api;
            } 
        }

        #endregion

        #region Métodos Públicos Estáticos

        /// <summary>
        /// Crea o modifica un documento DeCA mediante el API.
        /// </summary>
        /// <param name="document">DECa a remitir de alta.</param>
        /// <returns>Resultado llamada API.</returns>
        public static ExpandoObject Save(Document document) 
        {

            return Post(document, Api.EndPointSave);

        }

        /// <summary>
        /// Crea un registro de anulación mediante el API.
        /// </summary>
        /// <param name="document">DECa a anular.</param>
        /// <returns>Resultado llamada API.</returns>
        public static ExpandoObject Delete(Document document)
        {

            return Post(document, Api.EndPointDelete);

        }

        /// <summary>
        /// Recupera los registros envíados según el
        /// filtro pasado como parametro.
        /// </summary>
        /// <param name="filterSet">Filtro.</param>
        /// <returns>Resultado llamada API.</returns>
        public static ExpandoObject GetFilteredList(FilterSet filterSet)
        {

            return Post(filterSet, Api.EndPointGetFilteredList);

        }

        /// <summary>
        /// Crea Ct.
        /// </summary>
        /// <param name="log">Información técnica relevante.</param>
        /// <returns>Resultado llamada API.</returns>
        public static ExpandoObject Ct(string log = null)
        {

            try
            {

                var ct = new Ct(log);
                return Post(ct, Api.EndPointCt);

            }
            catch (Exception ex)
            {

                Utils.Log($"Error ApiClient.Ct:\n{ex.Message}");
                return null;

            }

        }

        #endregion

    }

}