# CAMSTEP: Guía de publicación

La app ya está preparada para producción:

- HTTPS obligatorio.
- Cookies seguras y cabeceras de seguridad.
- Página de error genérica.
- Las claves de las sesiones se guardan en disco.
- La base de datos se busca junto a la app.

Lo que falta es elegir **dónde** publicarla. Aquí tienes tres caminos, del más fácil al más completo.

> Antes de cualquier opción, **haz una copia** de `camstep.db` (y de los archivos `-wal` y `-shm`) y de la carpeta `App_Data`.

---

## Opción A: Abrirla en tu celular, en tu misma red WiFi (sin publicar)

Sirve para probarla en el celular o mostrarla en una reunión.

1. En Visual Studio, al lado del botón ▶, elige el perfil **"CAMSTEP (red local - celular)"** y pulsa F5.
2. Si Windows pregunta por el **Firewall**, permite el acceso en **redes privadas**.
3. Busca la IP de tu PC: abre `cmd`, escribe `ipconfig` y copia la **Dirección IPv4** (por ejemplo `192.168.1.25`).
4. En el celular, conectado al mismo WiFi, abre `http://192.168.1.25:5070`.

La app solo está disponible mientras tu PC esté encendido y Visual Studio esté corriéndola.

---

## Opción B: Hosting gratuito para .NET (MonsterASP.NET)

MonsterASP.NET tiene un plan gratuito, sin tarjeta, con soporte para .NET 8 y un subdominio gratis del tipo `tusitio.runasp.net`. Las condiciones del plan pueden cambiar, así que revísalas en su página antes de empezar.

1. Crea la cuenta en https://www.monsterasp.net y crea un sitio web.
2. En el panel del sitio, descarga el **perfil de publicación** (Publish profile / Web Deploy). Si no lo encuentras, busca "deploy Visual Studio" en https://help.monsterasp.net.
3. En Visual Studio, clic derecho sobre el proyecto → **Publicar** → **Importar perfil** → elige el archivo que descargaste.
4. Antes de publicar, en **Configuración**:
   - Configuración: **Release**
   - Framework: **net8.0**
5. Pulsa **Publicar**.
6. **La primera vez:** la app crea una base de datos vacía. Si quieres subir la tuya, usa el administrador de archivos del panel (o FTP) y copia `camstep.db` a la carpeta raíz del sitio, donde está `Camstep.dll`. Copia también tu carpeta `App_Data` si quieres conservar tus fondos y tu música.
7. Abre `https://tusitio.runasp.net`.

**Importante:**

- La publicación **no reemplaza** `camstep.db`, `App_Data` ni `backups` (no forman parte del proyecto que se publica), así que tus datos quedan a salvo al volver a publicar. Aun así, descarga una copia de `camstep.db` de vez en cuando.
- La carpeta del sitio debe permitir **escribir archivos**. Si al registrarte aparece un error, revisa los permisos en el panel o pregunta en el soporte del hosting.
- En los planes gratuitos la app puede "dormirse" cuando nadie la usa. Los avisos se ponen al día cuando alguien vuelve a entrar.

---

## Opción C: Azure App Service (plan gratuito F1)

El plan F1 de Microsoft es gratuito. Tiene recursos limitados, no permite dominio propio (la dirección queda como `tusitio.azurewebsites.net`) y no tiene "Always On". Eso significa que la app se apaga sin visitas, y los avisos en segundo plano solo corren mientras está encendida.

1. Crea una cuenta en https://azure.microsoft.com/free.
2. En el portal: **Crear un recurso** → **Aplicación web**.
   - Pila del entorno de ejecución: **.NET 8 (LTS)**
   - Sistema operativo: **Windows**
   - Plan: **Free F1**
3. En Visual Studio: clic derecho sobre el proyecto → **Publicar** → **Azure** → **Azure App Service (Windows)** → elige tu app → **Publicar**.
4. En el portal, en **Configuración** → **Variables de entorno**, agrega:
   - `ConnectionStrings__DefaultConnection` = `Data Source=D:\home\data\camstep\camstep.db`

   Así la base queda en el almacenamiento permanente de Azure (`D:\home`) y no se pierde al volver a publicar. Si la carpeta no existe, la app la crea al arrancar.
5. Abre `https://tusitio.azurewebsites.net`.

---

## Configurar el correo (para "¿Olvidaste tu contraseña?")

Sin correo configurado, la recuperación de contraseña **no envía nada**. En modo desarrollo, el enlace aparece en pantalla para que puedas probar.

**Con Gmail:**

1. En tu cuenta de Google, activa la **verificación en 2 pasos**.
2. Crea una **contraseña de aplicación** en https://myaccount.google.com/apppasswords.
3. En el hosting, agrega estas **variables de entorno** (no las escribas en `appsettings.json`, porque son secretas):

| Variable | Valor |
| --- | --- |
| `Smtp__Host` | `smtp.gmail.com` |
| `Smtp__Port` | `587` |
| `Smtp__EnableSsl` | `true` |
| `Smtp__User` | `tucorreo@gmail.com` |
| `Smtp__Password` | la contraseña de aplicación (16 letras) |
| `Smtp__From` | `tucorreo@gmail.com` |

Para probarlo en tu PC sin subir la contraseña a ningún lado, usa los **secretos de usuario** de Visual Studio: clic derecho sobre el proyecto → **Administrar secretos de usuario**, y pega esto:

```json
{
  "Smtp": { "Host": "smtp.gmail.com", "Port": 587, "EnableSsl": true,
            "User": "tucorreo@gmail.com", "Password": "xxxx xxxx xxxx xxxx", "From": "tucorreo@gmail.com" }
}
```

---

## Publicar desde la terminal (cualquier hosting)

```bash
dotnet publish -c Release -o publish
```

La carpeta `publish/` es la que subes al servidor. Al ejecutarse ahí, la app crea `camstep.db`, `App_Data/` y `backups/` junto a `Camstep.dll`.

Para probarla en tu PC exactamente como quedará en producción:

```bash
cd publish
set ASPNETCORE_ENVIRONMENT=Production
dotnet Camstep.dll --urls "http://localhost:5080;https://localhost:5443"
```

Abre `http://localhost:5080`: debe redirigirte a `https://localhost:5443`.

---

## Lista de verificación al publicar

- [ ] La página abre por **https://**.
- [ ] Puedes registrarte, salir y volver a entrar.
- [ ] Subes un fondo y una canción, y se ven y se escuchan.
- [ ] Reinicias el sitio desde el panel y la sesión y los datos siguen ahí.
- [ ] "¿Olvidaste tu contraseña?" envía el correo (si lo configuraste).
- [ ] La app abre bien desde el celular, con datos móviles.
- [ ] Descargaste una copia de `camstep.db`.
