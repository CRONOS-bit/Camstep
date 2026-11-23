# CAMSTEP

Aplicación web personal de organización: sendero de progreso, modo enfoque, hábitos que crecen, ánimo diario, domingo de calma, frases del camino, panel principal, tareas, calendario (mes, semana y día) con eventos, finanzas (gastos, ingresos, presupuestos, informes), metas con pasos, avisos, buscador, música con listas de reproducción, personalización y perfil.

**Tecnologías:** C#, ASP.NET Core MVC (.NET 8), Entity Framework Core 8 y SQLite. No usa librerías de JavaScript.

## Cómo ejecutarla

1. Abre `Camstep.sln` con Visual Studio.
2. Pulsa F5. Visual Studio restaura los paquetes la primera vez.
3. Se abre en `http://localhost:5070`.

Para abrirla desde el celular en la misma red WiFi, usa el perfil **"CAMSTEP (red local - celular)"**.

## Documentación

- `docs/MANUAL_DE_USUARIO.md`: cómo usar cada sección.
- `docs/DOCUMENTACION_TECNICA.md`: arquitectura, modelo de datos, seguridad, configuración y pruebas.
- `docs/GUIA_DE_PUBLICACION.md`: cómo publicarla en internet y cómo configurar el correo.
- `docs/diagramas/`: los diagramas en PNG.

## Datos y archivos

- `camstep.db`: la base de datos SQLite. Si no existe, se crea vacía al arrancar.
- `backups/`: copias automáticas de la base, que se crean antes de agregar tablas o columnas nuevas.
- `App_Data/uploads/{usuario}/`: los fondos y la música de cada usuario. Están fuera de `wwwroot` y solo los puede ver su dueño.
- `App_Data/keys/`: las claves que protegen las cookies de sesión.

## Sobre el nombre

La app se llama **CAMSTEP** (CAM de *camino* + STEP, *paso*). Si vienes de una versión anterior que usaba `vidaenorden.db`, al arrancar la app la copia sola a `camstep.db` y deja la original como respaldo.

## Acerca de nosotros: contacto

La página **Acerca de** (`/About`) es pública. El correo y los enlaces de GitHub/LinkedIn se configuran en `appsettings.json`, sección `About`. Si quedan vacíos, esas líneas no se muestran. Los enlaces deben empezar por `https://`.

## Sobre la música

La aplicación no descarga música de Spotify, YouTube ni de otras plataformas. Cada usuario sube archivos de audio que tenga derecho a usar.

También se pueden escuchar enlaces de **SoundCloud** con su reproductor oficial (no se descarga nada; solo se guarda el enlace).

La **música ambiente** (`wwwroot/audio/camstep-ambiente.mp3`) es original: se generó por código para CAMSTEP, sin muestras de terceros. Si prefieres otra pista, reemplaza ese archivo por un MP3 con el mismo nombre.

Las fuentes Quicksand e Inter están incluidas en `wwwroot/fonts` con su licencia libre (SIL OFL), así la app no depende de internet para verse bien.
