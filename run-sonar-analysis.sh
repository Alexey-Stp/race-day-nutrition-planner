#!/bin/bash
# Run SonarCloud Analysis Locally

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting SonarCloud Analysis...${NC}"

# Check if SONAR_TOKEN is set
if [ -z "$SONAR_TOKEN" ]; then
    echo -e "${RED}Error: SONAR_TOKEN environment variable is not set${NC}"
    echo "Please set it with: export SONAR_TOKEN=your_token"
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
