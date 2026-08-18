<img width="625" height="320" alt="image" src="https://github.com/user-attachments/assets/d056fbff-e39d-4df9-bae2-365c51125983" />

# DeCA
Librería .NET y API REST de código abierto para la generación, validación y gestión del Documento Electrónico de Control Administrativo (DeCA). Diseñada para facilitar su integración en ERP, TMS y aplicaciones de gestión del transporte.

<br>

> ### La funcionalidad de DeCA está disponible ( :wink: gratis) también en línea:
>
> :globe_with_meridians: [Acceso al API REST](https://deca.irenesolutions.com)
> 
> Con el API REST disponemos de una herramienta de trabajo sencilla sin la complicación de gestionar un repositorio central de documentos, su publicación en Internet, disponibilidad y caducidad.


<br>
<br>

# Creación de un documento DeCA en PDF

El siguiente ejemplo crea un documento **DeCA**, lo convierte a PDF utilizando la plantilla incluida en la librería y lo guarda en disco.

> ### Gestión local del documento
>
> En este ejemplo la generación y gestión del documento se realiza **íntegramente en local mediante la librería DeCA**.
>
> La aplicación que integra la librería es responsable no solo de generar y almacenar el PDF, sino también de implementar la infraestructura necesaria para su disponibilidad posterior a través de Internet.
>
> En particular, si se desea utilizar el código QR del DeCA como mecanismo de acceso al documento, la aplicación deberá encargarse de:
>
> - Generar y mantener una URL pública de acceso al documento.
> - Incluir dicha URL en `DownloadURL` y como valor del código QR.
> - Publicar y conservar el PDF en un servidor accesible desde Internet.
> - Controlar el período durante el cual el documento debe permanecer disponible.
> - Gestionar la caducidad del acceso cuando finalice dicho período.
> - Garantizar la persistencia y disponibilidad de las distintas versiones del documento cuando corresponda.
>
> La librería proporciona las herramientas necesarias para generar el DeCA y su PDF, pero **la publicación, almacenamiento remoto y control del período de disponibilidad son responsabilidad de la aplicación que la integra**.

```csharp
using System.IO;

// Crear el documento DeCA.
Document document = new Document()
{
    // Identificación del documento.
    OwnerPartyID = "B12959755",
    DeCAID = "DECA-2026-000001",
    DocumentNumber = "DECA-2026-000001",
    Version = 0,
    Status = "DRAFT",

    // Fechas del documento.
    CreationDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
    ModificationDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
    IssueDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
    TransportDate = new DateTime(2026, 8, 1),

    // Mercancía.
    GoodsDescription = "Componentes electrónicos",
    GoodsQuantity = 1000.00m,
    GoodsQuantityUnitCode = "KGM",
    GrossWeight = 500.00m,
    IsDangerousGoods = true,

    // Vehículo.
    TractorRegistrationNumber = "MHJ3322Z",
    TrailerRegistrationNumber = "BBJ3322Z",
    SecondTrailerRegistrationNumber = "AAJ3322Z",
    SpecialCirculationAuthorizationNumber = "99-99",

    // Información adicional.
    Remarks = "Documento de ejemplo generado con DeCA.",
    ModificationReason = "Creación del documento de prueba",

    // Acceso digital.
    DownloadURL = "https://github.com/mdiago/DeCA",
    QRCodeValue = "https://github.com/mdiago/DeCA",

    // Intervinientes y lugares del transporte.
    Parties = new List<Party>()
    {
        // Cargador contractual.
        new Party()
        {
            PartyRole = "CC",
            PartyID = "B12959755",
            FullName = "IRENE SOLUTIONS SL",
            TaxID = "B12959755",
            Address = "PZ ESTANY COLOMBRI, 3B",
            PostalCode = "12530",
            City = "BURRIANA",
            Region = "CASTELLÓN",
            CountryID = "ES",
            Mail = "info@irenesolutions.com",
            Phone = "+34 964 000 000"
        },

        // Transportista efectivo.
        new Party()
        {
            PartyRole = "TE",
            PartyID = "B44531218",
            FullName = "WEFINZ SOLUTIONS SL",
            TaxID = "B44531218",
            Address = "AV CAMINO DE ONDA, 25",
            PostalCode = "12530",
            City = "BURRIANA",
            Region = "CASTELLÓN",
            CountryID = "ES",
            Mail = "transportes@wefinz.com",
            Phone = "+34 964 111 111"
        },

        // Expedidor o cargador efectivo.
        new Party()
        {
            PartyRole = "EX",
            PartyID = "WH01",
            FullName = "ALMACÉN CENTRAL",
            Address = "POL. INDUSTRIAL CARABONA, NAVE 12",
            PostalCode = "12530",
            City = "BURRIANA",
            Region = "CASTELLÓN",
            CountryID = "ES"
        },

        // Destinatario de la mercancía.
        new Party()
        {
            PartyRole = "DS",
            PartyID = "B12345678",
            FullName = "CLIENTE DE EJEMPLO SL",
            TaxID = "B12345678",
            Address = "C/ MAYOR, 12",
            PostalCode = "28001",
            City = "MADRID",
            Region = "MADRID",
            CountryID = "ES",
            Mail = "recepcion@cliente.es",
            Phone = "+34 915 000 000"
        },

        // Lugar de origen del transporte.
        new Party()
        {
            PartyRole = "OR",
            FullName = "MUELLE DE CARGA 3",
            Address = "POL. INDUSTRIAL CARABONA",
            PostalCode = "12530",
            City = "BURRIANA",
            Region = "CASTELLÓN",
            CountryID = "ES"
        },

        // Lugar de destino del transporte.
        new Party()
        {
            PartyRole = "DE",
            FullName = "PLATAFORMA LOGÍSTICA MADRID SUR",
            Address = "AV. DE LA LOGÍSTICA, 45",
            PostalCode = "28906",
            City = "GETAFE",
            Region = "MADRID",
            CountryID = "ES"
        }
    }
};

// Generar el PDF utilizando la plantilla incluida en la librería.
DeCAPdfConverter converter = new DeCAPdfConverter(document);

// Guardar el documento PDF.
File.WriteAllBytes("DeCA.pdf", converter.GetPdf());
```

# Creación de un documento DeCA utilizando el API REST

El siguiente ejemplo crea un documento **DeCA** utilizando el API REST.

> ### Gestión automática mediante el API REST
>
> Al utilizar el **API REST de DeCA**, toda la infraestructura necesaria para la generación, publicación y disponibilidad del documento se gestiona automáticamente.
>
> La aplicación cliente únicamente debe enviar los datos necesarios para crear el DeCA. El servicio se encarga de:
>
> - Asignar la identificación y versión del documento.
> - Generar el PDF definitivo.
> - Generar el código QR.
> - Crear la URL pública de acceso al documento.
> - Almacenar y publicar el PDF.
> - Mantener disponibles los documentos y sus versiones.
> - Controlar automáticamente el período de disponibilidad y la caducidad del acceso.
>
> Por tanto, al utilizar el API REST **no es necesario implementar un repositorio público de documentos ni desarrollar mecanismos propios para gestionar su publicación y caducidad**.
>
> Una vez creado el documento, la respuesta del servicio contiene en `Return.DownloadURL` la URL desde la que puede visualizarse el DeCA.

```csharp

    // Crear el documento DeCA.
    Document document = new Document()
    {
        // Identificación del documento.
        OwnerPartyID = "B12959755",
        Version = 0,
        Status = "DRAFT",

        // Fechas del documento.
        CreationDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
        ModificationDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
        IssueDateTime = new DateTime(2026, 7, 31, 10, 30, 00),
        TransportDate = new DateTime(2026, 8, 1),

        // Mercancía.
        GoodsDescription = "Componentes electrónicos",
        GoodsQuantity = 1000.00m,
        GoodsQuantityUnitCode = "KGM",
        GrossWeight = 500.00m,
        IsDangerousGoods = true,

        // Vehículo.
        TractorRegistrationNumber = "MHJ3322Z",
        TrailerRegistrationNumber = "BBJ3322Z",
        SecondTrailerRegistrationNumber = "AAJ3322Z",
        SpecialCirculationAuthorizationNumber = "99-99",

        // Información adicional.
        Remarks = "Documento de ejemplo generado con DeCA.",
        ModificationReason = "Creación del documento de prueba",


        // Intervinientes y lugares del transporte.
        Parties = new List<Party>()
        {
            // Cargador contractual.
            new Party()
            {
                PartyRole = "CC",
                PartyID = "B12959755",
                FullName = "IRENE SOLUTIONS SL",
                TaxID = "B12959755",
                Address = "PZ ESTANY COLOMBRI, 3B",
                PostalCode = "12530",
                City = "BURRIANA",
                Region = "CASTELLÓN",
                CountryID = "ES",
                Mail = "info@irenesolutions.com",
                Phone = "+34 964 000 000"
            },

            // Transportista efectivo.
            new Party()
            {
                PartyRole = "TE",
                PartyID = "B44531218",
                FullName = "WEFINZ SOLUTIONS SL",
                TaxID = "B44531218",
                Address = "AV CAMINO DE ONDA, 25",
                PostalCode = "12530",
                City = "BURRIANA",
                Region = "CASTELLÓN",
                CountryID = "ES",
                Mail = "transportes@wefinz.com",
                Phone = "+34 964 111 111"
            },

            // Expedidor o cargador efectivo.
            new Party()
            {
                PartyRole = "EX",
                PartyID = "WH01",
                FullName = "ALMACÉN CENTRAL",
                Address = "POL. INDUSTRIAL CARABONA, NAVE 12",
                PostalCode = "12530",
                City = "BURRIANA",
                Region = "CASTELLÓN",
                CountryID = "ES"
            },

            // Destinatario de la mercancía.
            new Party()
            {
                PartyRole = "DS",
                PartyID = "B12345678",
                FullName = "CLIENTE DE EJEMPLO SL",
                TaxID = "B12345678",
                Address = "C/ MAYOR, 12",
                PostalCode = "28001",
                City = "MADRID",
                Region = "MADRID",
                CountryID = "ES",
                Mail = "recepcion@cliente.es",
                Phone = "+34 915 000 000"
            },

            // Lugar de origen del transporte.
            new Party()
            {
                PartyRole = "OR",
                FullName = "MUELLE DE CARGA 3",
                Address = "POL. INDUSTRIAL CARABONA",
                PostalCode = "12530",
                City = "BURRIANA",
                Region = "CASTELLÓN",
                CountryID = "ES"
            },

            // Lugar de destino del transporte.
            new Party()
            {
                PartyRole = "DE",
                FullName = "PLATAFORMA LOGÍSTICA MADRID SUR",
                Address = "AV. DE LA LOGÍSTICA, 45",
                PostalCode = "28906",
                City = "GETAFE",
                Region = "MADRID",
                CountryID = "ES"
            }
        }
    };

    dynamic result = ApiClient.Save(document);

    if (result.ResultCode != 0)
    {

        Debug.Print($"Se ha producido un error al llamar al API: {result.ResultMessage}");

    }
    else
    {

        var url = $"{result.Return.DownloadURL}";
        Debug.Print($"La url de acceso al documento DeCA es: {url}");

              

    }


```
El PDF generado incluirá automáticamente:

- Información de identificación y control del documento.
- Cargador contractual.
- Transportista efectivo.
- Expedidor o cargador efectivo.
- Destinatario de la mercancía.
- Lugar de origen del transporte.
- Lugar de destino del transporte.
- Información de la mercancía.
- Datos del vehículo.
- Observaciones y modificaciones.
- Código QR y datos para la descarga del documento.
