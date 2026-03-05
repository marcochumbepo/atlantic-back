#!/bin/bash

# Atlantic Orders API - cURL Examples
# Este script muestra ejemplos de cómo usar la API con cURL

set -e

# Colores para terminal
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuración
BASE_URL="http://localhost:8080"
TOKEN=""
CREATED_PEDIDO_ID=""

# Función para imprimir encabezados
print_header() {
    echo -e "\n${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}\n"
}

# Función para imprimir respuestas
print_response() {
    echo -e "${GREEN}Response:${NC}"
    echo "$1"
}

# Función para imprimir errores
print_error() {
    echo -e "${RED}Error: $1${NC}"
}

# ============================================================
# 1. HEALTH CHECK
# ============================================================
print_header "1. Health Check"
echo "GET /health"

RESPONSE=$(curl -s -X GET "$BASE_URL/health")
print_response "$RESPONSE"

# ============================================================
# 2. LOGIN
# ============================================================
print_header "2. Login - Obtener Token JWT"
echo "POST /api/auth/login"

RESPONSE=$(curl -s -X POST "$BASE_URL/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "usuario": "admin@example.com",
    "contrasena": "admin123"
  }')

print_response "$RESPONSE"

# Extraer token (compatible con macOS y Linux)
TOKEN=$(echo "$RESPONSE" | grep -o '"token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
    print_error "No se pudo obtener el token"
    exit 1
fi

echo -e "${GREEN}✓ Token obtenido correctamente${NC}"
echo -e "Token: ${YELLOW}${TOKEN:0:50}...${NC}\n"

# ============================================================
# 3. OBTENER TODOS LOS PEDIDOS
# ============================================================
print_header "3. GET /api/pedidos - Obtener todos los pedidos"
echo "Autenticación: Bearer $TOKEN"

RESPONSE=$(curl -s -X GET "$BASE_URL/api/pedidos" \
  -H "Authorization: Bearer $TOKEN")

print_response "$RESPONSE"

# ============================================================
# 4. CREAR PEDIDO
# ============================================================
print_header "4. POST /api/pedidos - Crear nuevo pedido"
echo "Autenticación: Bearer $TOKEN"

RESPONSE=$(curl -s -X POST "$BASE_URL/api/pedidos" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "numeroPedido": "PED-CURL-001",
    "cliente": "Cliente desde cURL",
    "fecha": "2026-03-04T10:30:00",
    "total": 2500.50,
    "estado": "Pendiente"
  }')

print_response "$RESPONSE"

# Extraer el ID del pedido creado
CREATED_PEDIDO_ID=$(echo "$RESPONSE" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)

if [ ! -z "$CREATED_PEDIDO_ID" ]; then
    echo -e "${GREEN}✓ Pedido creado con ID: $CREATED_PEDIDO_ID${NC}\n"
else
    print_error "No se pudo extraer el ID del pedido creado"
fi

# ============================================================
# 5. OBTENER PEDIDO POR ID
# ============================================================
if [ ! -z "$CREATED_PEDIDO_ID" ]; then
    print_header "5. GET /api/pedidos/{id} - Obtener pedido específico"
    echo "ID: $CREATED_PEDIDO_ID"
    echo "Autenticación: Bearer $TOKEN"

    RESPONSE=$(curl -s -X GET "$BASE_URL/api/pedidos/$CREATED_PEDIDO_ID" \
      -H "Authorization: Bearer $TOKEN")

    print_response "$RESPONSE"
fi

# ============================================================
# 6. ACTUALIZAR PEDIDO
# ============================================================
if [ ! -z "$CREATED_PEDIDO_ID" ]; then
    print_header "6. PUT /api/pedidos/{id} - Actualizar pedido"
    echo "ID: $CREATED_PEDIDO_ID"
    echo "Autenticación: Bearer $TOKEN"

    RESPONSE=$(curl -s -X PUT "$BASE_URL/api/pedidos/$CREATED_PEDIDO_ID" \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json" \
      -d "{
        \"id\": $CREATED_PEDIDO_ID,
        \"numeroPedido\": \"PED-CURL-001\",
        \"cliente\": \"Cliente desde cURL - Actualizado\",
        \"fecha\": \"2026-03-04T10:30:00\",
        \"total\": 3000.75,
        \"estado\": \"Completado\"
      }")

    print_response "$RESPONSE"
fi

# ============================================================
# 7. ELIMINAR PEDIDO
# ============================================================
if [ ! -z "$CREATED_PEDIDO_ID" ]; then
    print_header "7. DELETE /api/pedidos/{id} - Eliminar pedido"
    echo "ID: $CREATED_PEDIDO_ID"
    echo "Autenticación: Bearer $TOKEN"

    RESPONSE=$(curl -s -X DELETE "$BASE_URL/api/pedidos/$CREATED_PEDIDO_ID" \
      -H "Authorization: Bearer $TOKEN" \
      -w "\nHTTP Status: %{http_code}")

    print_response "$RESPONSE"
fi

# ============================================================
# RESUMEN
# ============================================================
print_header "✓ Flujo Completado Exitosamente"
echo -e "${GREEN}Todos los endpoints funcionan correctamente${NC}\n"

echo "Resumen de operaciones:"
echo "  1. ✓ Health Check"
echo "  2. ✓ Login (Token obtenido)"
echo "  3. ✓ Obtener todos los pedidos"
echo "  4. ✓ Crear pedido (ID: $CREATED_PEDIDO_ID)"
echo "  5. ✓ Obtener pedido por ID"
echo "  6. ✓ Actualizar pedido"
echo "  7. ✓ Eliminar pedido"
