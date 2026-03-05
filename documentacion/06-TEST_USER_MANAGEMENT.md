# Gestión de Usuarios de Prueba - Atlantic Orders

Guía completa para gestionar usuarios de prueba en la API de Atlantic Orders.

## Inicio Rápido

Los usuarios de prueba se crean automáticamente cuando el contenedor Docker inicia. Puedes comenzar a usarlos inmediatamente sin configuración adicional.

## Usuarios de Prueba Predeterminados

Los siguientes usuarios de prueba se sembrarán automáticamente en la base de datos al iniciar la aplicación:

| Correo Electrónico | Contraseña | Rol | Propósito |
|-------------------|------------|-----|-----------|
| `test@example.com` | `Test@123456` | Usuario | Pruebas generales |
| `admin@example.com` | `Admin@123456` | Administrador | Pruebas de operaciones de administrador |
| `demo@example.com` | `Demo@123456` | Usuario | Propósitos de demostración |

## Crear Usuarios de Prueba

### Opción 1: Siembra Automática (Recomendado)

Los usuarios de prueba se crean automáticamente cuando el contenedor Docker inicia si no existen.

**Cómo funciona**:
1. El contenedor inicia y inicializa la base de datos
2. Program.cs verifica si la tabla de Usuarios está vacía
3. Si está vacía, crea los tres usuarios de prueba con contraseñas hasheadas con BCrypt
4. Registra mensajes de confirmación en la salida del contenedor

**Ver registros de siembra**:
```bash
docker logs atlantic-orders-api | grep "Sembrando\|usuarios de prueba"
```

**Ejemplo de salida**:
```
info: Program[0]
      Sembrando datos de prueba en la base de datos...
info: Program[0]
      ✓ 3 usuarios de prueba creados exitosamente
info: Program[0]
        - test@example.com / Test@123456
info: Program[0]
        - admin@example.com / Admin@123456
info: Program[0]
        - demo@example.com / Demo@123456
```

### Opción 2: Reiniciar Base de Datos

Si necesitas reiniciar a los usuarios de prueba predeterminados:

```bash
# Detener y eliminar contenedor (la base de datos en memoria se eliminará)
docker-compose down

# Reiniciar contenedor (la siembra recreará los usuarios de prueba)
docker-compose up -d

# Esperar unos segundos para que la API esté lista
sleep 3

# Verificar que los usuarios fueron creados
docker logs atlantic-orders-api | grep "usuarios de prueba"
```

### Opción 3: Crear Usuarios de Prueba Adicionales (Vía API)

Actualmente, no hay endpoint para crear usuarios. Para agregar más usuarios de prueba, necesitarías:

**Opción A: Modificar Program.cs**
1. Editar `AtlanticOrders.Api/Program.cs` alrededor de la línea 214
2. Agregar nuevos objetos User a la lista de usuarios de prueba
3. Reconstruir la imagen Docker: `docker-compose up -d --build`

**Ejemplo**:
```csharp
new User
{
    Email = "newuser@example.com",
    PasswordHash = passwordHasher.HashPassword("Password@12345"),
    FullName = "Nuevo Usuario de Prueba",
    Role = "Usuario",
    IsActive = true,
    CreatedAt = DateTime.UtcNow
}
```

**Opción B: Modificación Directa de la Base de Datos**
1. Conectarse a la base de datos en memoria (requiere cambios en la API)
2. Insertar registro de usuario con contraseña hasheada con BCrypt

## Probar con Usuarios Predeterminados

### Iniciar Sesión con Usuario de Prueba

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123456"
  }'
```

**Respuesta**:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3599,
  "refreshToken": "EO4mwm5/PMGmUha3uCrE5eQ..."
}
```

### Probar con los Tres Usuarios

```bash
#!/bin/bash

test_user() {
  local email=$1
  local password=$2
  
  response=$(curl -s -X POST http://localhost:8080/api/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$email\",\"password\":\"$password\"}")
  
  if echo "$response" | grep -q '"token"'; then
    echo "✓ Inicio de sesión exitoso para $email"
  else
    echo "✗ Inicio de sesión fallido para $email"
  fi
}

test_user "test@example.com" "Test@123456"
test_user "admin@example.com" "Admin@123456"
test_user "demo@example.com" "Demo@123456"
```

## Propiedades del Usuario

Cada usuario de prueba tiene las siguientes propiedades:

```json
{
  "id": 1,                                    // Auto-incrementado
  "email": "test@example.com",               // Único, usado para iniciar sesión
  "passwordHash": "$2a$12$...",              // Hash BCrypt (factor de trabajo 12)
  "fullName": "Usuario de Prueba",           // Nombre para mostrar
  "role": "Usuario",                         // "Usuario" o "Administrador"
  "isActive": true,                          // Estado de la cuenta
  "createdAt": "2026-03-05T15:34:00Z",        // UTC ISO 8601
  "updatedAt": null,                         // Opcional
  "lastLoginAt": null                        // Actualizado al iniciar sesión
}
```

## Requisitos de Contraseña

Las contraseñas deben cumplir estos criterios:
- **Longitud**: Sin mínimo establecido (para propósitos de prueba)
- **Validación**: Ocurre en el inicio de sesión mediante verificación BCrypt
- **Hasheo**: BCrypt con factor de trabajo 12 (2^12 rondas)
- **Almacenamiento**: Solo se almacena el hash, nunca texto plano

Las contraseñas de prueba usadas son simples para demostración:
- `Test@123456` para usuario regular
- `Admin@123456` para administrador
- `Demo@123456` para usuario de demostración

**Nota de Producción**: Implementar requisitos de contraseña más fuertes (mínimo 12 caracteres, reglas de complejidad, etc.)

## Consideraciones de Seguridad

### ✅ Lo que está Implementado

- **Hasheo BCrypt**: Las contraseñas se hashean con factor de trabajo 12
- **Salting**: Cada contraseña tiene una sal única
- **Sin Almacenamiento de Texto Plano**: Solo se almacenan los hashes
- **Valores Predeterminados Seguros**: test@example.com incluido en datos de siembra

### ⚠️ Notas de Seguridad para Pruebas

- Las contraseñas de prueba son simples y visibles en el código
- Los usuarios de prueba existen solo para desarrollo/pruebas
- La base de datos en memoria se reinicia cuando el contenedor se reinicia
- Nunca usar contraseñas de prueba en producción

### Recomendaciones de Producción

1. **Eliminar siembra automática** en producción
2. **Implementar endpoint de registro de usuarios** con validación
3. **Aplicar contraseñas fuertes** (12+ caracteres, mayúsculas, números, símbolos)
4. **Agregar verificación de correo electrónico** antes de la activación de la cuenta
5. **Implementar limitación de velocidad** en el registro
6. **Registrar toda creación de usuarios** para auditoría

## Solución de Problemas

### Los Usuarios de Prueba No Existen

**Síntoma**: El inicio de sesión devuelve "Credenciales inválidas" para usuarios de prueba

**Causas**:
1. La base de datos no fue sembrada (verificar si el contenedor inició correctamente)
2. La base de datos está usando SQL Server en lugar de en memoria (verificar appsettings)
3. El código de siembra no se ejecutó (verificar registros)

**Soluciones**:
```bash
# Verificar si el contenedor está ejecutándose
docker ps | grep atlantic

# Verificar registros de siembra
docker logs atlantic-orders-api | grep -i "sembrando\|error"

# Reiniciar contenedor (volverá a sembrar si usa en memoria)
docker-compose restart atlantic-orders-api

# Esperar a que inicie
sleep 3

# Probar inicio de sesión
docker exec atlantic-orders-api curl -s http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123456"}'
```

### Mensajes de Error Diferentes

**Síntoma**: Mensajes de error diferentes a los esperados

**Posibles Problemas**:
- La contraseña puede ser sensible a mayúsculas (lo es)
- El correo electrónico puede requerir coincidencia exacta (lo requiere)
- La base de datos puede tener usuarios de prueba antiguos (reiniciar contenedor)

**Pasos de Depuración**:
```bash
# Probar cada usuario individualmente
for email in "test@example.com" "admin@example.com" "demo@example.com"; do
  echo "Probando: $email"
  docker exec atlantic-orders-api curl -s -X POST http://localhost:8080/api/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$email\",\"password\":\"Test@123456\"}" \
    | grep -o '"error":"[^"]*"' || echo "Éxito"
done
```

### Problemas de Limitación de Velocidad

**Síntoma**: Obteniendo "429 Too Many Requests" después de pocos intentos

**Causa**: La limitación de velocidad es de 5 solicitudes por minuto por IP

**Solución**:
```bash
# Esperar 60 segundos para que el límite se reinicie
sleep 60

# O usar diferentes usuarios de prueba para distribuir solicitudes
# (Cada correo electrónico puede contarse de manera diferente)
```

## Persistencia de la Base de Datos

### Base de Datos en Memoria (Predeterminada)

- Los datos solo existen mientras el contenedor está ejecutándose
- Se reinicia completamente cuando el contenedor se detiene/reinicia
- Los usuarios de prueba se recrean al iniciar
- Perfecto para desarrollo/pruebas

### SQL Server (Opcional)

Si está configurado para usar SQL Server:
1. Los datos persisten entre reinicios del contenedor
2. Los usuarios de prueba solo se crean una vez (en la primera ejecución)
3. Debes reiniciar manualmente si es necesario

**Verificar modo actual de base de datos**:
```bash
grep "UseInMemoryDatabase" docker-compose.yml
```

**Cambiar a SQL Server**:
```yaml
# En docker-compose.yml
environment:
  UseInMemoryDatabase: "false"  # Cambiar a false
  ConnectionStrings__DefaultConnection: "Server=sql-server;Database=Atlantic;..."
```

## Inspección de la Base de Datos

### Ver Código de Siembra

La lógica de siembra está en `Program.cs` alrededor de la línea 194:

```csharp
// Sembrar datos de prueba si no existen usuarios
if (!dbContext.Users.Any())
{
    var logger = app.Services.GetService<ILogger<Program>>();
    logger?.LogInformation("Sembrando datos de prueba en la base de datos...");
    
    var testUsers = new List<User>
    {
        // Usuarios de prueba definidos aquí
    };
    
    dbContext.Users.AddRange(testUsers);
    await dbContext.SaveChangesAsync();
}
```

### Verificar Cantidad de Usuarios

```bash
# Desde dentro del contenedor
docker exec atlantic-orders-api curl -s http://localhost:8080/api/users/count
```

### Acceso Directo a la Base de Datos

Como la base de datos es en memoria, el acceso directo requiere:
1. Exponer un endpoint personalizado
2. O usar el profiler de Entity Framework
3. O modificar el código para consultar usuarios

## Flujos de Trabajo de Pruebas

### Flujo 1: Prueba de Autenticación Básica

```bash
#!/bin/bash
echo "1. Iniciar sesión con usuario de prueba"
response=$(curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test@123456"}')

token=$(echo "$response" | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -n "$token" ]; then
  echo "✓ Inicio de sesión exitoso"
  echo "2. Usando token para solicitud autenticada"
  
  curl -X GET http://localhost:8080/api/protected \
    -H "Authorization: Bearer $token"
else
  echo "✗ Inicio de sesión fallido"
  echo "Respuesta: $response"
fi
```

### Flujo 2: Prueba de Múltiples Usuarios

```bash
#!/bin/bash
users=(
  "test@example.com:Test@123456"
  "admin@example.com:Admin@123456"
  "demo@example.com:Demo@123456"
)

for user_pair in "${users[@]}"; do
  IFS=':' read -r email password <<< "$user_pair"
  
  response=$(curl -s -X POST http://localhost:8080/api/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$email\",\"password\":\"$password\"}")
  
  if echo "$response" | grep -q '"token"'; then
    echo "✓ $email autenticado exitosamente"
  else
    echo "✗ $email autenticación fallida"
  fi
done
```

## Próximos Pasos

Para despliegue en producción:

1. **Deshabilitar siembra automática** en `Program.cs`
2. **Implementar endpoint** de registro de usuarios
3. **Agregar flujo de verificación** de correo electrónico
4. **Migrar a SQL Server** con estrategia de respaldo
5. **Implementar endpoint** de restablecimiento de contraseña
6. **Agregar endpoints** de gestión de usuarios (listar, actualizar, eliminar)
7. **Implementar control de acceso basado en roles** (RBAC)

## Referencias

- [CURL_TESTING_GUIDE.md](./CURL_TESTING_GUIDE.md) - Guía completa de pruebas de API
- [SECURITY_SETUP.md](../backend/SECURITY_SETUP.md) - Detalles de seguridad del backend
- Documentación de BCrypt: https://github.com/BcryptNET/bcrypt.net-core
- Hoja de referencia de OWASP para Almacenamiento de Contraseñas: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
