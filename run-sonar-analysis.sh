#!/bin/bash
# Run SonarCloud Analysis Locally

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting SonarCloud Analysis...${NC}"

# Load .env file if it exists (for local development)
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
fi

# In CI/CD, SONAR_TOKEN comes from GitHub Secrets
if [ -z "$SONAR_TOKEN" ]; then
    echo -e "${RED}Error: SONAR_TOKEN is not set${NC}"
    echo "For local development: Create a .env file with SONAR_TOKEN=your_token"
    echo "For CI/CD: Token is provided via GitHub Secrets"
    exit 1
fi

# Install SonarScanner if not present
if ! command -v dotnet-sonarscanner &> /dev/null; then
    echo -e "${YELLOW}Installing SonarScanner for .NET...${NC}"
    dotnet tool install --global dotnet-sonarscanner
fi

# Clean previous build
echo -e "${YELLOW}Cleaning previous builds...${NC}"
dotnet clean

# Begin SonarCloud analysis
echo -e "${YELLOW}Beginning SonarCloud analysis...${NC}"
dotnet sonarscanner begin \
  /k:"Alexey-Stp_race-day-nutrition-planner" \
  /o:"alexey-stp" \
  /d:sonar.host.url="https://sonarcloud.io" \
  /d:sonar.login="$SONAR_TOKEN" \
  /d:sonar.cs.opencover.reportsPaths="coverage/**/coverage.opencover.xml"

# Build the project
echo -e "${YELLOW}Building project...${NC}"
dotnet build --no-incremental

# Run tests with coverage
echo -e "${YELLOW}Running tests with coverage...${NC}"
dotnet test \
  --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# End SonarCloud analysis
echo -e "${YELLOW}Completing SonarCloud analysis...${NC}"
dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"

echo -e "${GREEN}Analysis complete! Check results at: https://sonarcloud.io${NC}"
