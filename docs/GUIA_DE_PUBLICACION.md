# CAMSTEP: Guía de publicación

La app ya está preparada para producción:

- HTTPS obligatorio.
- Cookies seguras y cabeceras de seguridad.
- Página de error genérica.
- Las claves de las sesiones se guardan en disco.
- Con la variable `DataRoot`, la base, los archivos subidos, las claves y los respaldos se guardan juntos en una carpeta aparte (por ejemplo un volumen del servidor).
- Trae un `Dockerfile` listo para servidores que usan contenedores, como Railway.

Lo que falta es elegir **dónde** publicarla. Para tenerla abierta 24/7 para cualquier persona, la recomendada es **Railway** (abajo). Las otras opciones sirven para probar o para hostings gratuitos con limitaciones.

> Antes de cualquier opción, **haz una copia** de `camstep.db` (y de los archivos `-wal` y `-shm`) y de la carpeta `App_Data`.

---

## Recomendada: Railway (24/7, desde GitHub)

Railway toma tu repositorio de GitHub, construye la app con el `Dockerfile` y la publica con HTTPS. Cada vez que haces `git push`, se vuelve a publicar sola.

**Costo** (revísalo en https://railway.com/pricing, porque puede cambiar):

- Prueba gratis de 30 días con 5 USD de crédito, sin tarjeta.
- Después, el plan gratis da 1 USD al mes y las apps **se duermen** cuando nadie las usa. El volumen es de solo 0.5 GB.
- Para que esté despierta 24/7, el plan **Hobby** cuesta 5 USD al mes (incluye 5 USD de uso) y el volumen llega hasta 5 GB.

### Paso 1: Sube los cambios a GitHub

```bash
git add .
git commit -m "Preparar CAMSTEP para Railway"
git push
```

Revisa en GitHub que se vean `Dockerfile` y `.dockerignore` en la raíz, junto a `Camstep.csproj`.

### Paso 2: Crea el proyecto

1. Entra a https://railway.com y pulsa **Login** → **Login with GitHub**.
2. **New Project** → **Deploy from GitHub repo** → elige tu repositorio de CAMSTEP. Si no aparece, pulsa **Configure GitHub App** y dale acceso a ese repositorio.
3. Railway detecta el `Dockerfile` solo y empieza a construir. La primera vez tarda unos minutos.

### Paso 3: Agrega el volumen (¡no te lo saltes!)

Sin volumen, **cada nueva publicación borra todas las cuentas y los datos**.

1. En el lienzo del proyecto, clic derecho sobre el servicio (o **Ctrl+K**) → **Add Volume**.
2. Elige tu servicio y en **Mount path** escribe exactamente: `/data`
3. Railway reinicia la app. Desde ahí, `camstep.db`, la música, los fondos, las claves de sesión y los respaldos viven en `/data`.

### Paso 4: Variables

En el servicio → pestaña **Variables** → **New Variable**:

| Variable | Valor | ¿Obligatoria? |
| --- | --- | --- |
| `About__ContactEmail` | tu correo de contacto | Muy recomendada: aparece en "Acerca de" y en la política de privacidad |
| `About__GitHub` | enlace a tu GitHub | Opcional |
| `About__LinkedIn` | enlace a tu LinkedIn | Opcional |
| `Storage__MusicQuotaMB` | `40` en el plan gratis, `150` en Hobby | Recomendada: espacio de música por usuario |
| `Smtp__Host`, `Smtp__Port`, `Smtp__EnableSsl`, `Smtp__User`, `Smtp__Password`, `Smtp__From` | ver "Configurar el correo" | Para "¿Olvidaste tu contraseña?" |
| `Security__MaxRegistrationsPerHour` | por defecto `5` | Opcional: cuentas nuevas por hora desde una misma conexión |

`DataRoot=/data` y el puerto ya vienen en el `Dockerfile`; no los pongas.

### Paso 5: Genera la dirección

1. En el servicio → **Settings** → **Networking** → **Generate Domain**.
2. Te da algo como `camstep-production.up.railway.app`, ya con HTTPS.
3. Si algún día compras un dominio propio, ahí mismo está **Custom Domain**.

### Paso 6: Prueba

Recorre la lista de verificación del final de esta guía, desde el celular con datos móviles.

### Bueno saberlo

- **La app arranca vacía en el servidor.** Tu `camstep.db` de tu PC no se sube (está en `.gitignore` y `.dockerignore`), así que nadie verá tus datos de prueba.
- **Cada publicación cierra las sesiones abiertas.** Las sesiones viven en memoria; después de un `git push` todos vuelven a iniciar sesión. Los datos no se pierden.
- **Respaldos:** antes de cualquier cambio de estructura, la app guarda una copia en `/data/backups`. Para bajar una copia de la base puedes entrar con la CLI de Railway (`railway ssh`).
- **Registros (logs):** en el servicio → **Deployments** → **View logs**.
- **Gasto:** en **Workspace → Usage** ves cuánto vas consumiendo. Puedes poner un límite de gasto para no llevarte sorpresas.

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
- [ ] La página de **Privacidad** muestra tu correo de contacto.
- [ ] Publicas otra vez (un `git push`) y tus datos siguen ahí.
- [ ] Descargaste una copia de `camstep.db`.
