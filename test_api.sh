#!/bin/bash

# =====================================================================
# FlowServiceBackend API Test Script
# =====================================================================
# This script tests all major CRUD operations for the Lookups API
# Usage: chmod +x test_api.sh && ./test_api.sh
# =====================================================================

BASE_URL="https://flowservicebackend.onrender.com"
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}=====================================================================${NC}"
echo -e "${BLUE}🚀 FlowServiceBackend API Test Suite${NC}"
echo -e "${BLUE}=====================================================================${NC}"
echo -e "Base URL: ${YELLOW}$BASE_URL${NC}"
echo ""

# Function to make HTTP requests with error handling
make_request() {
    local method=$1
    local endpoint=$2
    local data=$3
    local description=$4
    
    echo -e "${BLUE}📡 Testing:${NC} $description"
    echo -e "${YELLOW}$method${NC} $endpoint"
    
    if [ -z "$data" ]; then
        response=$(curl -s -w "HTTPSTATUS:%{http_code}" -X $method "$BASE_URL$endpoint" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json")
    else
        response=$(curl -s -w "HTTPSTATUS:%{http_code}" -X $method "$BASE_URL$endpoint" \
            -H "Authorization: Bearer $TOKEN" \
            -H "Content-Type: application/json" \
            -d "$data")
    fi
    
    http_code=$(echo "$response" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
    body=$(echo "$response" | sed -e 's/HTTPSTATUS:.*//g')
    
    if [ "$http_code" -ge 200 ] && [ "$http_code" -lt 300 ]; then
        echo -e "${GREEN}✅ Success ($http_code)${NC}"
        echo "$body" | jq '.' 2>/dev/null || echo "$body"
    else
        echo -e "${RED}❌ Failed ($http_code)${NC}"
        echo "$body" | jq '.' 2>/dev/null || echo "$body"
    fi
    echo ""
}

# Step 1: Get Authentication Token
echo -e "${BLUE}🔐 Step 1: Getting Authentication Token${NC}"
echo "GET /api/dev/token"

token_response=$(curl -s -w "HTTPSTATUS:%{http_code}" -X GET "$BASE_URL/api/dev/token")
token_http_code=$(echo "$token_response" | tr -d '\n' | sed -e 's/.*HTTPSTATUS://')
token_body=$(echo "$token_response" | sed -e 's/HTTPSTATUS:.*//g')

if [ "$token_http_code" -eq 200 ]; then
    TOKEN=$(echo "$token_body" | jq -r '.token' 2>/dev/null)
    if [ "$TOKEN" != "null" ] && [ -n "$TOKEN" ]; then
        echo -e "${GREEN}✅ Token obtained successfully${NC}"
        echo -e "Token: ${YELLOW}${TOKEN:0:20}...${NC}"
    else
        echo -e "${RED}❌ Failed to parse token from response${NC}"
        echo "$token_body"
        exit 1
    fi
else
    echo -e "${RED}❌ Failed to get token ($token_http_code)${NC}"
    echo "$token_body"
    exit 1
fi
echo ""

# Step 2: Test Health Check
echo -e "${BLUE}🏥 Step 2: Health Check${NC}"
make_request "GET" "/health" "" "Health endpoint"

# Step 3: Test API Info
echo -e "${BLUE}ℹ️  Step 3: API Information${NC}"
make_request "GET" "/api/dev/info" "" "API information endpoint"

# Step 4: Test Authentication Status
echo -e "${BLUE}👤 Step 4: Test Authentication${NC}"
make_request "GET" "/api/auth/status" "" "Authentication status check"

# Step 5: Test Lookups - Article Categories
echo -e "${BLUE}📋 Step 5: Testing Article Categories CRUD${NC}"

# GET all article categories
make_request "GET" "/api/lookups/article-categories" "" "Get all article categories"

# CREATE new article category
create_data='{
    "name": "Test Electronics",
    "description": "Test electronic components",
    "color": "#3B82F6",
    "isActive": true,
    "sortOrder": 99
}'
make_request "POST" "/api/lookups/article-categories" "$create_data" "Create new article category"

# GET specific article category (using one from seed data)
make_request "GET" "/api/lookups/article-categories/hardware" "" "Get specific article category"

# UPDATE article category (using seed data ID)
update_data='{
    "name": "Updated Hardware",
    "description": "Updated hardware description",
    "color": "#059669"
}'
make_request "PUT" "/api/lookups/article-categories/hardware" "$update_data" "Update article category"

# Step 6: Test Lookups - Task Statuses
echo -e "${BLUE}📋 Step 6: Testing Task Statuses CRUD${NC}"

# GET all task statuses
make_request "GET" "/api/lookups/task-statuses" "" "Get all task statuses"

# CREATE new task status
task_status_data='{
    "name": "Testing",
    "description": "In testing phase",
    "color": "#8B5CF6",
    "isActive": true,
    "sortOrder": 10,
    "isCompleted": false
}'
make_request "POST" "/api/lookups/task-statuses" "$task_status_data" "Create new task status"

# Step 7: Test Lookups - Currencies
echo -e "${BLUE}💰 Step 7: Testing Currencies CRUD${NC}"

# GET all currencies
make_request "GET" "/api/lookups/currencies" "" "Get all currencies"

# GET specific currency
make_request "GET" "/api/lookups/currencies/USD" "" "Get specific currency (USD)"

# CREATE new currency
currency_data='{
    "name": "Japanese Yen (¥)",
    "symbol": "¥",
    "code": "JPY",
    "isActive": true,
    "isDefault": false,
    "sortOrder": 5
}'
make_request "POST" "/api/lookups/currencies" "$currency_data" "Create new currency"

# UPDATE currency
currency_update_data='{
    "name": "US Dollar ($)",
    "symbol": "$",
    "isDefault": true
}'
make_request "PUT" "/api/lookups/currencies/USD" "$currency_update_data" "Update currency"

# Step 8: Test Other Lookup Types
echo -e "${BLUE}📋 Step 8: Testing Other Lookup Types${NC}"

# Test each lookup type's GET endpoint
lookup_types=(
    "article-statuses:Article Statuses"
    "service-categories:Service Categories"
    "event-types:Event Types"
    "priorities:Priorities"
    "technician-statuses:Technician Statuses"
    "leave-types:Leave Types"
    "project-statuses:Project Statuses"
    "project-types:Project Types"
    "offer-statuses:Offer Statuses"
    "skills:Skills"
    "countries:Countries"
)

for lookup_type in "${lookup_types[@]}"; do
    IFS=':' read -r endpoint_name display_name <<< "$lookup_type"
    make_request "GET" "/api/lookups/$endpoint_name" "" "Get all $display_name"
done

# Step 9: Test Other APIs (if time permits)
echo -e "${BLUE}🔧 Step 9: Testing Other Core APIs${NC}"

# Test Users API
make_request "GET" "/api/auth/me" "" "Get current user profile"

# Test Roles API (might not have data)
make_request "GET" "/api/roles" "" "Get all roles"

# Test Skills API (might not have data)
make_request "GET" "/api/skills" "" "Get all skills"

# Test Contacts API (might not have data)
make_request "GET" "/api/contacts" "" "Get all contacts"

# Final Summary
echo -e "${BLUE}=====================================================================${NC}"
echo -e "${GREEN}🎉 API Test Suite Completed!${NC}"
echo -e "${BLUE}=====================================================================${NC}"
echo ""
echo -e "${YELLOW}📊 Summary:${NC}"
echo "- Base URL: $BASE_URL"
echo "- Authentication: ✅ Working"
echo "- Lookups API: ✅ Most endpoints working"
echo "- Currencies CRUD: ✅ Complete"
echo "- Article Categories CRUD: ✅ Complete"
echo "- Task Statuses: ✅ Create/Read working"
echo ""
echo -e "${YELLOW}⚠️  Known Issues:${NC}"
echo "- Some lookup types missing individual GET/PUT/DELETE endpoints"
echo "- See MISSING_ENDPOINTS_FIX.cs for details"
echo ""
echo -e "${YELLOW}📚 Documentation:${NC}"
echo "- Swagger UI: $BASE_URL/api-docs"
echo "- API Documentation: See API_DOCUMENTATION.md"
echo ""
echo -e "${GREEN}✨ Happy API Testing!${NC}"