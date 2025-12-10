#!/bin/bash

###############################################################################
# Race Day Nutrition Planner - Quick Test Script
#
# This script provides an easy way to test nutrition plan generation with
# predefined scenarios. Use this to generate plans and review them with experts.
#
# Usage:
#   ./test-nutrition-plan.sh [scenario]
#
# Available scenarios:
#   half-triathlon  - Half Triathlon 4:30 (90kg, Race Intensity)
#   full-triathlon  - Full Triathlon 10 hours (90kg, Race Intensity)
#   half-marathon   - Half Marathon 21km (90kg, Race Intensity)
#   full-marathon   - Full Marathon 42km (90kg, Race Intensity)
#   bike-4h         - Bike 4 hours (90kg, Race Intensity)
#   all             - Run all scenarios sequentially
#
# Examples:
#   ./test-nutrition-plan.sh half-triathlon
#   ./test-nutrition-plan.sh all
#
# Prerequisites:
#   - API server must be running on http://localhost:5208
#   - Start it with: dotnet run --project src/RaceDay.API/RaceDay.API.csproj
###############################################################################

API_URL="http://localhost:5208/api/plan/generate"

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print section header
print_header() {
    echo -e "\n${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}\n"
}

# Function to check if API is running
check_api() {
    if ! curl -s -f -o /dev/null "$API_URL" -X POST -H "Content-Type: application/json" -d '{}' 2>/dev/null; then
        echo -e "${YELLOW}Warning: API might not be running on http://localhost:5208${NC}"
        echo -e "${YELLOW}Start the API with: dotnet run --project src/RaceDay.API/RaceDay.API.csproj${NC}\n"
    fi
}

# Scenario 1: Half Triathlon 4:30
test_half_triathlon() {
    print_header "Scenario: Half Triathlon 4:30 hours (90kg, Race Intensity)"
    
    curl -s -X POST "$API_URL" \
      -H "Content-Type: application/json" \
      -d '{
        "athleteWeightKg": 90,
        "sportType": "Triathlon",
        "durationHours": 4.5,
        "temperatureC": 20,
        "intensity": "Hard",
        "products": [
          {
            "name": "Maurten GEL 100",
            "productType": "gel",
            "carbsG": 25,
            "sodiumMg": 85,
            "volumeMl": 40
          },
          {
            "name": "Maurten Drink Mix 320 (500ml)",
            "productType": "drink",
            "carbsG": 80,
            "sodiumMg": 345,
            "volumeMl": 500
          }
        ]
      }' | python3 -m json.tool
    
    echo -e "${GREEN}✓ Half Triathlon test completed${NC}"
}

# Scenario 2: Full Triathlon 10 hours
test_full_triathlon() {
    print_header "Scenario: Full Triathlon 10 hours (90kg, Race Intensity)"
    
    curl -s -X POST "$API_URL" \
      -H "Content-Type: application/json" \
      -d '{
        "athleteWeightKg": 90,
        "sportType": "Triathlon",
        "durationHours": 10,
        "temperatureC": 22,
        "intensity": "Hard",
        "products": [
          {
            "name": "Maurten GEL 100",
            "productType": "gel",
            "carbsG": 25,
            "sodiumMg": 85,
            "volumeMl": 40
          },
          {
            "name": "Maurten Drink Mix 320 (500ml)",
            "productType": "drink",
            "carbsG": 80,
            "sodiumMg": 345,
            "volumeMl": 500
          },
          {
            "name": "Energy Bar",
            "productType": "bar",
            "carbsG": 35,
            "sodiumMg": 150,
            "volumeMl": 0
          }
        ]
      }' | python3 -m json.tool
    
    echo -e "${GREEN}✓ Full Triathlon test completed${NC}"
}

# Scenario 3: Half Marathon 21km
test_half_marathon() {
    print_header "Scenario: Half Marathon 21km (~2 hours, 90kg, Race Intensity)"
    
    curl -s -X POST "$API_URL" \
      -H "Content-Type: application/json" \
      -d '{
        "athleteWeightKg": 90,
        "sportType": "Run",
        "durationHours": 2,
        "temperatureC": 18,
        "intensity": "Hard",
        "products": [
          {
            "name": "SiS GO Isotonic Energy Gel",
            "productType": "gel",
            "carbsG": 22,
            "sodiumMg": 10,
            "volumeMl": 60
          },
          {
            "name": "SiS GO Electrolyte Drink (500ml)",
            "productType": "drink",
            "carbsG": 36,
            "sodiumMg": 300,
            "volumeMl": 500
          }
        ]
      }' | python3 -m json.tool
    
    echo -e "${GREEN}✓ Half Marathon test completed${NC}"
}

# Scenario 4: Full Marathon 42km
test_full_marathon() {
    print_header "Scenario: Full Marathon 42km (~4 hours, 90kg, Race Intensity)"
    
    curl -s -X POST "$API_URL" \
      -H "Content-Type: application/json" \
      -d '{
        "athleteWeightKg": 90,
        "sportType": "Run",
        "durationHours": 4,
        "temperatureC": 20,
        "intensity": "Hard",
        "products": [
          {
            "name": "Maurten GEL 100",
            "productType": "gel",
            "carbsG": 25,
            "sodiumMg": 85,
            "volumeMl": 40
          },
          {
            "name": "Maurten Drink Mix 320 (500ml)",
            "productType": "drink",
            "carbsG": 80,
            "sodiumMg": 345,
            "volumeMl": 500
          }
        ]
      }' | python3 -m json.tool
    
    echo -e "${GREEN}✓ Full Marathon test completed${NC}"
}

# Scenario 5: Bike 4 hours
test_bike_4h() {
    print_header "Scenario: Bike 4 hours (90kg, Race Intensity)"
    
    curl -s -X POST "$API_URL" \
      -H "Content-Type: application/json" \
      -d '{
        "athleteWeightKg": 90,
        "sportType": "Bike",
        "durationHours": 4,
        "temperatureC": 22,
        "intensity": "Hard",
        "products": [
          {
            "name": "SiS Beta Fuel Gel",
            "productType": "gel",
            "carbsG": 40,
            "sodiumMg": 200,
            "volumeMl": 60
          },
          {
            "name": "SiS GO Electrolyte Drink (500ml)",
            "productType": "drink",
            "carbsG": 36,
            "sodiumMg": 300,
            "volumeMl": 500
          }
        ]
      }' | python3 -m json.tool
    
    echo -e "${GREEN}✓ Bike 4 hours test completed${NC}"
}

# Main script logic
main() {
    check_api
    
    case "${1:-}" in
        half-triathlon)
            test_half_triathlon
            ;;
        full-triathlon)
            test_full_triathlon
            ;;
        half-marathon)
            test_half_marathon
            ;;
        full-marathon)
            test_full_marathon
            ;;
        bike-4h)
            test_bike_4h
            ;;
        all)
            test_half_triathlon
            test_full_triathlon
            test_half_marathon
            test_full_marathon
            test_bike_4h
            ;;
        *)
            echo "Usage: $0 [scenario]"
            echo ""
            echo "Available scenarios:"
            echo "  half-triathlon  - Half Triathlon 4:30 (90kg, Race Intensity)"
            echo "  full-triathlon  - Full Triathlon 10 hours (90kg, Race Intensity)"
            echo "  half-marathon   - Half Marathon 21km (90kg, Race Intensity)"
            echo "  full-marathon   - Full Marathon 42km (90kg, Race Intensity)"
            echo "  bike-4h         - Bike 4 hours (90kg, Race Intensity)"
            echo "  all             - Run all scenarios sequentially"
            echo ""
            echo "Examples:"
            echo "  $0 half-triathlon"
            echo "  $0 all"
            exit 1
            ;;
    esac
}

main "$@"
