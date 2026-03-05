# Atlantic Orders Backend - Índice de Documentación

## Bienvenido a la Documentación

Esta carpeta contiene documentación completa para el **Atlantic Orders Backend** - una API REST de calidad profesional construida con Arquitectura Limpia, mejores prácticas de seguridad y patrones listos para producción.

**Estado de la Documentación**: ✅ Completo (7 documentos)  
**Última Actualización**: 5 de Marzo de 2026  
**Audiencia**: Desarrolladores Senior, Tech Leads, Entrevistadores Técnicos

---

## 📋 Navegación Rápida

### Para Diferentes Roles:

- **👨‍💻 Desarrolladores Configurando Localmente**: Comenzar con [01-INSTALLATION_GUIDE.md](#01-guía-de-instalación)
- **🐳 DevOps/Despliegue**: Comenzar con [02-DOCKER_DEPLOYMENT.md](#02-guía-de-despliegue-docker)
- **🔌 Integración API**: Comenzar con [03-POSTMAN_COLLECTION.json](#03-colección-postman)
- **🏗️ Revisión de Arquitectura**: Comenzar con [04-ARCHITECTURE.md](#04-documentación-de-arquitectura)
- **💡 Preparación para Entrevistas**: Comenzar con [05-TECHNICAL_DECISIONS.md](#05-decisiones-técnicas)
- **👤 Gestión de Usuarios de Prueba**: Comenzar con [06-TEST_USER_MANAGEMENT.md](#06-gestión-de-usuarios-de-prueba)

---

## 📚 Guía de Documentos

### 01. Guía de Instalación
**Archivo**: `01-INSTALLATION_GUIDE.md`  
**Tiempo de Lectura**: 15 minutos  
**Cuándo Leer**: Configurando entorno de desarrollo

**Contiene**:
- ✅ Prerrequisitos y requisitos de software
- ✅ Proceso de instalación paso a paso
- ✅ Configuración de secretos de usuario (claves API, cadenas de conexión)
- ✅ Opciones de configuración de base de datos:
  - En memoria (para pruebas)
  - LocalDB (desarrollo local)
  - Docker SQL Server (similar a producción)
- ✅ Configuración de IDE para VS Code y Visual Studio 2022
- ✅ Ejecutando la aplicación localmente
- ✅ Solución de problemas comunes

**Inicio Rápido**:
```bash
# Clonar y navegar
git clone <repo>
cd Atlantic/backend

# Instalar dependencias
dotnet restore

# Configurar secretos
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "your-secret-key-min-64-chars"
dotnet user-secrets set "Jwt:Issuer" "atlantic-orders"
dotnet user-secrets set "Jwt:Audience" "atlantic-orders-app"

# Ejecutar
dotnet run
```

---

### 02. Guía de Despliegue Docker
**Archivo**: `02-DOCKER_DEPLOYMENT.md`  
**Tiempo de Lectura**: 20 minutos  
**Cuándo Leer**: Desplegando con Docker o docker-compose

**Contiene**:
- ✅ Inicio rápido con docker-compose
- ✅ Comandos del ciclo de vida de Docker (up, down, restart, logs)
- ✅ Verificaciones de salud y verificación de contenedores
- ✅ Variables de entorno y configuración .env
- ✅ Opciones de persistencia de base de datos (montaje de volúmenes)
- ✅ Explicación del Dockerfile (builds multi-etapa)
- ✅ Patrones de red y comunicación
- ✅ Optimización de rendimiento (límites de CPU, memoria)
- ✅ Mejores prácticas de seguridad (privilegios de usuario, gestión de secretos)
- ✅ Lista de verificación de despliegue en producción
- ✅ Configuración de proxy reverso (ejemplo con Nginx)
- ✅ Guía completa de solución de problemas

**Inicio Rápido**:
```bash
# Navegar al backend
cd backend

# Iniciar con docker-compose
docker-compose up -d

# Verificar salud
curl http://localhost:8080/api/health

# Ver logs
docker-compose logs -f api

# Detener
docker-compose down
```

---

### 03. Colección Postman
**Archivo**: `03-POSTMAN_COLLECTION.json`  
**Tiempo de Lectura**: 10 minutos  
**Cuándo Leer**: Probando endpoints de API

**Contiene**:
- ✅ Endpoints de autenticación pre-configurados
  - Login (obtener tokens)
  - Refresh token (extender sesión)
  - Logout (invalidar tokens)
- ✅ Endpoint de verificación de salud
- ✅ Escenarios de prueba de rate limiting (6 solicitudes para verificar)
- ✅ Variables de entorno (base_url, access_token, refresh_token)
- ✅ Scripts de auto-guardado de tokens (login exitoso guarda tokens automáticamente)
- ✅ Scripts de prueba para validación
- ✅ Soporte para casos de éxito y fracaso

**Cómo Usar**:
1. Importar `03-POSTMAN_COLLECTION.json` en Postman
2. Configurar variables de entorno:
   - `base_url`: http://localhost:8080 (o tu servidor)
3. Ejecutar solicitud "Login" para obtener tokens
4. Tokens se guardan automáticamente en variables de entorno
5. Otras solicitudes injectan tokens automáticamente

**Usuarios de Prueba**:
```
Email                  Contraseña      Rol
────────────────────────────────────────────
test@example.com      Test@123456    Usuario
admin@example.com     Admin@123456   Admin
demo@example.com      Demo@123456    Usuario
```

---

### 04. Documentación de Arquitectura
**Archivo**: `04-ARCHITECTURE.md`  
**Tiempo de Lectura**: 30 minutos  
**Cuándo Leer**: Entendiendo diseño del sistema y patrones

**Contiene**:
- ✅ Resumen de Arquitectura Limpia (patrón de 4 capas)
- ✅ Responsabilidades de cada capa:
  - Capa de Dominio (entidades, reglas de negocio)
  - Capa de Aplicación (servicios, DTOs, validadores)
  - Capa de Infraestructura (repositorios, persistencia, seguridad)
  - Capa de Presentación (controladores, middleware)
- ✅ Patrones de diseño implementados:
  - Patrón Repositorio (abstracción de acceso a datos)
  - Inyección de Dependencias (acoplamiento débil)
  - Patrón DTO (contratos API)
  - Patrón Capa de Servicio (lógica de negocio)
  - Patrón Fábrica (generación de tokens)
- ✅ Detalles del stack tecnológico
- ✅ Diagramas de flujo de datos:
  - Flujo de login (cómo funciona la autenticación)
  - Flujo de solicitud autenticada (validación de tokens)
- ✅ Diagramas de componentes (relaciones entre entidades)
- ✅ Opciones de escalabilidad:
  - Escalamiento horizontal (balanceo de carga)
  - Escalamiento de base de datos (sharding)
- ✅ Consideraciones de rendimiento
- ✅ Seguridad en cada capa
- ✅ Estrategias de testabilidad
- ✅ Enfoque de mantenimiento y logging

**Visualización de Arquitectura**:
```
┌─────────────────────────────────────┐
│   Presentación (Controladores API)  │
├─────────────────────────────────────┤
│   Aplicación (Servicios, DTOs)     │
├─────────────────────────────────────┤
│   Dominio (Lógica, Entidades)       │
├─────────────────────────────────────┤
│   Infraestructura (Datos, Seguridad)│
└─────────────────────────────────────┘
```

**Punto Clave**: Entender esta arquitectura es esencial para:
- Navegación del código
- Agregar nuevas funcionalidades
- Tomar decisiones arquitectónicas
- Explicar decisiones de diseño en entrevistas

---

### 05. Decisiones Técnicas
**Archivo**: `05-TECHNICAL_DECISIONS.md`  
**Tiempo de Lectura**: 45 minutos  
**Cuándo Leer**: Preparación para entrevistas, entendiendo el "por qué" detrás de las decisiones

**¡Este es el documento más importante para entrevistas!**

**Contiene** (con justificación detallada):
- ✅ **Arquitectura Limpia** vs N-Tier vs Monolítica
  - Por qué: Independencia de frameworks, testabilidad, escalabilidad
  - Compensaciones: Más archivos, complejidad inicial
  - Cuándo usar: Todos los sistemas en producción

- ✅ **JWT + Refresh Tokens** vs Sesiones
  - Por qué: Sin estado, escalable, amigable para móviles
  - Compensaciones: No se puede revocar al instante, tokens más grandes
  - Seguridad: Rotación de tokens, cookies HttpOnly, almacenamiento de tokens

- ✅ **Rate Limiting** (5 req/min auth, 100 req/min API)
  - Por qué: Protección DDoS, prevención de fuerza bruta
  - Implementación: Ventana deslizante por IP
  - Compensaciones: Usuarios de VPN podrían ser bloqueados

- ✅ **BCrypt 12 Rondas** vs otros algoritmos de hash
  - Por qué: Lento, adaptativo, con salt
  - Comparación: SHA-256 (1M intentos/seg), BCrypt (10 intentos/seg)
  - Configuración: Por qué 12 rondas (100-200ms por login)

- ✅ **Rotación de Token** (nuevo refresh token en cada uso)
  - Por qué: Detecta robo de tokens, limita ventana de daño
  - Compensaciones: Más escrituras en BD

- ✅ **Patrón Repositorio**
  - Por qué: Abstracción de acceso a datos, testabilidad, flexibilidad
  - Beneficios: Fácil cambiar ORM, consultas centralizadas
  - Testing: Fácil hacer mock de repositorios

- ✅ **DTOs Explícitos**
  - Por qué: Contratos API, seguridad, versionado
  - Beneficios: No exponer hashes de contraseñas, notas internas
  - Versionado: UserDtoV1, UserDtoV2

- ✅ **Entity Framework Core** vs Dapper vs NHibernate
  - Por qué: Soporte LINQ, migraciones, relaciones, testabilidad
  - Rendimiento: EF Core ~100ms vs Dapper ~30ms (compensación aceptable)
  - Cuándo considerar Dapper: Consultas intensivas en lectura >10k/seg

- ✅ **Inyección de Dependencias Integrada**
  - Por qué: Simple, integrado, suficiente para nuestras necesidades
  - Vidas de servicio: Transient, Scoped, Singleton
  - Por qué DbContext es Scoped: Previene contaminación entre solicitudes

- ✅ **Manejo de Excepciones Basado en Middleware**
  - Por qué: Centralizado, consistente, reutilizable
  - Beneficios: Sin duplicación en controladores
  - Implementación: Excepciones personalizadas, middleware captura

- ✅ **CORS Estricto + Encabezados de Seguridad**
  - Por qué: Previene XSS, CSRF, clickjacking
  - Encabezados: CSP, X-Frame-Options, HSTS, etc.
  - Configuración: Lista blanca de orígenes permitidos

- ✅ **Pruebas Unit + Integración + E2E** (Piramide)
  - Por qué: Cobertura equilibrada, retroalimentación rápida, pruebas realistas
  - Proporción: 70% unit, 20% integración, 10% E2E
  - Herramientas: NUnit, Moq, WebApplicationFactory

**Oro de Entrevistas**:
Este documento responde preguntas como:
- "¿Por qué elegiste JWT sobre sesiones?"
- "¿Cómo manejaste la seguridad?"
- "¿Cuáles son las compensaciones de tu arquitectura?"
- "¿Cómo escalarías esto?"
- "¿Por qué usar repositorios?"

**Estrategia de Preparación**:
1. Leer cada sección dos veces
2. Entender las alternativas
3. Practicar explicando compensaciones
4. Estar listo para defender decisiones
5. Discutir cuándo cada patrón puede fallar

---

### 06. Gestión de Usuarios de Prueba
**Archivo**: `06-TEST_USER_MANAGEMENT.md`  
**Tiempo de Lectura**: 15 minutos  
**Cuándo Leer**: Gestionando usuarios de prueba y datos de seeding

**Contiene**:
- ✅ Usuarios de prueba por defecto (3 usuarios)
- ✅ Proceso de sembrado automático (automatic seeding)
- ✅ Credenciales de prueba:
  - test@example.com / Test@123456 (Usuario)
  - admin@example.com / Admin@123456 (Admin)
  - demo@example.com / Demo@123456 (Usuario)
- ✅ Opciones para crear usuarios de prueba adicionales
- ✅ Scripts de testing con curl
- ✅ Workflows de prueba completos
- ✅ Consideraciones de seguridad para pruebas
- ✅ Solución de problemas comunes
- ✅ Persistencia de base de datos (in-memory vs SQL Server)

**Uso Rápido**:
```bash
# Login con usuario de prueba
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123456"
  }'
```

**Ver logs de sembrado**:
```bash
docker logs atlantic-orders-api | grep "Sembrando\|usuarios de prueba"
```

---

## 📄 Orden de Lectura Recomendado

### Para Nuevos Desarrolladores (Configurando localmente):
1. **01-INSTALLATION_GUIDE.md** - Obtener entorno funcionando
2. **04-ARCHITECTURE.md** - Entender estructura del código
3. **02-DOCKER_DEPLOYMENT.md** - Aprender despliegue (referencia futura)
4. **03-POSTMAN_COLLECTION.json** - Probar endpoints de API
5. **05-TECHNICAL_DECISIONS.md** - Profundizar después
6. **06-TEST_USER_MANAGEMENT.md** - Gestión de usuarios de prueba

### Para Ingenieros DevOps:
1. **02-DOCKER_DEPLOYMENT.md** - Enfoque principal
2. **01-INSTALLATION_GUIDE.md** - Entender configuración local (referencia)
3. **04-ARCHITECTURE.md** - Entender servicios/dependencias
4. **03-POSTMAN_COLLECTION.json** - Ejemplos de health check
5. **06-TEST_USER_MANAGEMENT.md** - Verificar usuarios de prueba

### Para Revisores de Código / Arquitectos:
1. **04-ARCHITECTURE.md** - Entender diseño
2. **05-TECHNICAL_DECISIONS.md** - Entender justificación
3. **01-INSTALLATION_GUIDE.md** - Configuración de pruebas locales
4. **03-POSTMAN_COLLECTION.json** - Referencia de endpoints
5. **06-TEST_USER_MANAGEMENT.md** - Entender seeding de datos

### Para Entrevistas de Trabajo:
1. **05-TECHNICAL_DECISIONS.md** - ENFOQUE PRINCIPAL
   - Leer 2-3 veces, practicar explicando
   - Entender compensaciones profundamente
   - Estar listo para discutir cuándo las decisiones fallan
2. **04-ARCHITECTURE.md** - Material de apoyo
   - Conocer los patrones usados
   - Entender flujos de datos
3. **01-INSTALLATION_GUIDE.md** - Si preguntan "¿cómo configurarías esto?"
4. **02-DOCKER_DEPLOYMENT.md** - Si preguntan sobre despliegue
5. **06-TEST_USER_MANAGEMENT.md** - Entender gestión de datos de prueba

---

## 🎯 Temas Clave por Documento

### ¿Buscando Temas Específicos?

**Autenticación y Seguridad**:
- ➡️ Leer: 05-TECHNICAL_DECISIONS.md → Sección 2, 3, 4, 5
- ➡️ Referencia Rápida: 04-ARCHITECTURE.md → Sección de Seguridad

**Base de Datos y ORM**:
- ➡️ Leer: 05-TECHNICAL_DECISIONS.md → Sección 8
- ➡️ Ver: 04-ARCHITECTURE.md → Capa de Infraestructura

**Diseño API y DTOs**:
- ➡️ Leer: 05-TECHNICAL_DECISIONS.md → Sección 7
- ➡️ Ver: 03-POSTMAN_COLLECTION.json → Ejemplos de Request/Response

**Estrategia de Pruebas**:
- ➡️ Leer: 05-TECHNICAL_DECISIONS.md → Sección 12
- ➡️ Ver: 04-ARCHITECTURE.md → Sección de Testabilidad
- ➡️ Ver: 06-TEST_USER_MANAGEMENT.md → Workflows de prueba

**Rendimiento y Escalabilidad**:
- ➡️ Leer: 04-ARCHITECTURE.md → Escalabilidad y Rendimiento
- ➡️ Profundizar: 05-TECHNICAL_DECISIONS.md → Comparar alternativas

**Despliegue y DevOps**:
- ➡️ Leer: 02-DOCKER_DEPLOYMENT.md
- ➡️ Configuración Local: 01-INSTALLATION_GUIDE.md

**Gestión de Datos de Prueba**:
- ➡️ Leer: 06-TEST_USER_MANAGEMENT.md (documento completo)
- ➡️ Ver: 03-POSTMAN_COLLECTION.json → Testing endpoints

---

## 📊 Cobertura de Documentación

```
Tema                       Nivel de Cobertura
────────────────────────────────────────────────
Instalación                 ★★★★★ Completo
Docker/Despliegue           ★★★★★ Completo
Endpoints API              ★★★★☆ Vía Postman
Arquitectura               ★★★★★ Completo
Patrones de Diseño         ★★★★★ Completo
Seguridad                  ★★★★★ Profundo
Autenticación              ★★★★★ Profundo
Base de Datos/ORM          ★★★★★ Profundo
Gestión de Usuarios        ★★★★★ Completo
Pruebas                    ★★★★☆ Bueno
Rendimiento                ★★★☆☆ Referencia
Monitoring/Logging         ★★★☆☆ Mención breve
CI/CD                      ★★☆☆☆ No cubierto
```

---

## 🚀 Lista de Verificación para Comenzar

- [ ] Leer 01-INSTALLATION_GUIDE.md
- [ ] Configurar entorno local
- [ ] Ejecutar `dotnet run`
- [ ] Importar 03-POSTMAN_COLLECTION.json
- [ ] Probar endpoints de login/refresh
- [ ] Leer 04-ARCHITECTURE.md
- [ ] Explorar código fuente con arquitectura en mente
- [ ] Leer 05-TECHNICAL_DECISIONS.md para comprensión profunda
- [ ] Leer 06-TEST_USER_MANAGEMENT.md para gestión de datos de prueba
- [ ] Practicar explicando decisiones de diseño

---

## ❓ Preguntas Frecuentes

### "¿Qué documento debo leer primero?"

**Depende de tu rol:**
- **Desarrollador**: Guía de Instalación → Arquitectura
- **DevOps**: Guía de Despliegue Docker
- **Entrevistador**: Decisiones Técnicas
- **Arquitecto**: Arquitectura → Decisiones Técnicas

### "¿Cuánto tiempo toma leer toda la documentación?"

- Instalación: 15 min
- Docker: 20 min
- Arquitectura: 30 min
- Decisiones Técnicas: 45 min
- Gestión de Usuarios: 15 min
- **Total: ~2.5 horas** (incluye lectura cuidadosa y tomar notas)

### "¿Me estoy preparando para una entrevista, qué es más importante?"

**05-TECHNICAL_DECISIONS.md** tiene el 80% del valor para entrevistas. Léelo cuidadosamente, entiende:
1. Por qué se tomó cada decisión
2. Qué alternativas existían
3. Compensaciones de cada enfoque
4. Cuándo las decisiones pueden fallar
5. Cómo lo explicarías a un ingeniero senior

### "¿Dónde encuentro ejemplos de código?"

- **Instalación**: 01-INSTALLATION_GUIDE.md (código de configuración)
- **Docker**: 02-DOCKER_DEPLOYMENT.md (docker-compose, Dockerfile)
- **Arquitectura**: 04-ARCHITECTURE.md (muchos ejemplos de código)
- **Decisiones Técnicas**: 05-TECHNICAL_DECISIONS.md (ejemplos extensos)
- **Gestión de Usuarios**: 06-TEST_USER_MANAGEMENT.md (workflows de prueba)
- **Código en Vivo**: Explorar carpeta `/backend/` directamente

### "¿Cómo pruebo la API?"

1. Importar `03-POSTMAN_COLLECTION.json` en Postman
2. Ejecutar endpoint "Login" con credenciales de usuario de prueba
3. Tokens se guardan automáticamente en variables de entorno
4. Usar otros endpoints (tokens injectados automáticamente)

**Credenciales de Prueba**:
```
Email: test@example.com
Contraseña: Test@123456
```

### "¿Puedo modificar estos documentos?"

Estos documentos son parte de tu portafolio. Siéntete libre de:
- ✅ Expandir secciones con más detalles
- ✅ Agregar información específica del proyecto
- ✅ Actualizar con nuevas funcionalidades que agregues
- ✅ Personalizar para tu audiencia
- ❌ No eliminar contenido central (es crítico para entrevistas)

---

## 📞 Mantenimiento de Documentos

**Última Actualización**: 5 de Marzo de 2026  
**Versión Actual**: 1.1 (Actualizado con documento de gestión de usuarios)  
**Mantenedor**: Leonardo (Creación inicial)

**Mantener Actualizado Cuando**:
- Agregar nuevos endpoints de API → Actualizar 03-POSTMAN_COLLECTION.json
- Cambiar autenticación → Actualizar 05-TECHNICAL_DECISIONS.md
- Modificar arquitectura → Actualizar 04-ARCHITECTURE.md
- Cambiar proceso de instalación → Actualizar 01-INSTALLATION_GUIDE.md
- Actualizar configuración Docker → Actualizar 02-DOCKER_DEPLOYMENT.md
- Agregar gestión de usuarios de prueba → Actualizar 06-TEST_USER_MANAGEMENT.md

---

## 🎓 Hoja de Ruta para Preparación de Entrevistas

### Semana 1: Fundamentos de Arquitectura
- [ ] Leer 04-ARCHITECTURE.md (completo)
- [ ] Entender capas de Arquitectura Limpia
- [ ] Explorar código fuente con arquitectura en mente
- [ ] Practicar dibujar diagrama de arquitectura

### Semana 2: Profundización en Decisiones de Diseño
- [ ] Leer 05-TECHNICAL_DECISIONS.md (completo)
- [ ] Enfocarse en secciones 1-5 (arquitectura, auth, seguridad)
- [ ] Escribir notas sobre alternativas para cada decisión
- [ ] Practicar explicar compensaciones

### Semana 3: Temas Específicos
- [ ] Profundizar en JWT + Refresh Tokens
- [ ] Entender BCrypt vs alternativas
- [ ] Estudiar implementación del Patrón Repositorio
- [ ] Revisar estrategia de rate limiting

### Semana 4: Práctica y Perfeccionamiento
- [ ] Practicar explicando cada decisión en 2-3 minutos
- [ ] Responder preguntas de "por qué" de 05-TECHNICAL_DECISIONS.md
- [ ] Discutir compensaciones con confianza
- [ ] Preparar walkthrough de diseño de sistema de 5 minutos

### Día de la Entrevista
- [ ] Revisar secciones 1-5 de 05-TECHNICAL_DECISIONS.md
- [ ] Estar listo para discutir:
  - Elección de arquitectura y por qué
  - Estrategia de autenticación y seguridad
  - Opciones de escalabilidad
  - Enfoque de pruebas
- [ ] Recorrer ejemplos de código
- [ ] Discutir compensaciones con confianza

---

## 📖 Recursos Adicionales

**Dentro de Este Repositorio**:
- `CURL_TESTING_GUIDE.md` - 50+ ejemplos de comandos curl
- `TEST_USER_MANAGEMENT.md` - Configuración de usuarios y seeding de base de datos
- `ENCRIPTACION_FRONTEND.md` - Arquitectura de seguridad del frontend

**Código Fuente**:
- `backend/` - Código fuente completo con comentarios en línea
- `front/` - Código del frontend y autenticación

**Referencias Externas**:
- [Arquitectura Limpia por Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Mejores Prácticas JWT](https://tools.ietf.org/html/rfc7519)
- [Guías de Seguridad OWASP](https://owasp.org/www-community/)
- [Documentación ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)

---

## ✨ Qué Hace Especial Esta Documentación

- ✅ **Lista para Entrevistas**: Escrita específicamente para entrevistas técnicas
- ✅ **Profundidad Técnica**: No solo "qué" sino "por qué" para cada decisión
- ✅ **Alternativas Discutidas**: Cada decisión incluye compensaciones
- ✅ **Ejemplos de Código**: Ejemplos de implementación real para cada concepto
- ✅ **Diagramas Visuales**: Arquitectura, flujos de datos, relaciones de componentes
- ✅ **Práctica**: Puede usarse inmediatamente como referencia para desarrollo
- ✅ **Completa**: Desde configuración hasta despliegue y decisiones arquitectónicas
- ✅ **Lista para Portafolio**: Adecuada para inclusión en solicitudes de trabajo

---

## 🎯 Conclusión

Este paquete de documentación está diseñado para:

1. **Obtener productividad rápidamente** - Guías de Instalación y Docker
2. **Ayudarte a entender el código base** - Documentación de Arquitectura
3. **Prepararte para entrevistas** - Decisiones técnicas con razonamiento profundo
4. **Servir como referencia** - Búsqueda rápida para arquitectura, endpoints, patrones
5. **Gestionar datos de prueba** - Documento de gestión de usuarios de prueba

**Dedica tiempo a 05-TECHNICAL_DECISIONS.md** - es lo más valioso para el crecimiento profesional y entrevistas.

---

**¡Feliz aprendizaje y buena suerte en tus entrevistas! 🚀**
