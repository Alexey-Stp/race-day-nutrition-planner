#!/bin/bash
# Fetch and display SonarCloud issues

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Load .env file if it exists (for local development)
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
fi

# In CI/CD, SONAR_TOKEN comes from GitHub Secrets
PROJECT_KEY="Alexey-Stp_race-day-nutrition-planner"

if [ -z "$SONAR_TOKEN" ]; then
    echo -e "${RED}Error: SONAR_TOKEN is not set${NC}"
    echo "For local development: Create a .env file with SONAR_TOKEN=your_token"
    echo "For CI/CD: Token is provided via GitHub Secrets"
    exit 1
fi

echo -e "${BLUE}=== SonarCloud Issues ===${NC}\n"

# Fetch issues
RESPONSE=$(curl -s "https://sonarcloud.io/api/issues/search?componentKeys=${PROJECT_KEY}&resolved=false&ps=100" \
  -H "Authorization: Bearer ${SONAR_TOKEN}")

# Parse and display
TOTAL=$(echo "$RESPONSE" | grep -o '"total":[0-9]*' | head -1 | cut -d: -f2)
echo -e "${GREEN}Total Issues: ${TOTAL}${NC}\n"

# Count by severity
CRITICAL=$(echo "$RESPONSE" | grep -o '"severity":"CRITICAL"' | wc -l | tr -d ' ')
MAJOR=$(echo "$RESPONSE" | grep -o '"severity":"MAJOR"' | wc -l | tr -d ' ')
MINOR=$(echo "$RESPONSE" | grep -o '"severity":"MINOR"' | wc -l | tr -d ' ')

echo -e "${RED}Critical: ${CRITICAL}${NC}"
echo -e "${YELLOW}Major: ${MAJOR}${NC}"
echo -e "${BLUE}Minor: ${MINOR}${NC}\n"

# Display issues grouped by file
echo -e "${BLUE}=== Issues by File ===${NC}\n"

echo "$RESPONSE" | python3 -c "
import sys, json

try:
    data = json.load(sys.stdin)
    issues = data.get('issues', [])
    
    # Group by component
    by_file = {}
    for issue in issues:
        comp = issue.get('component', '').split(':')[-1]
        if comp not in by_file:
            by_file[comp] = []
        by_file[comp].append(issue)
    
    # Display grouped issues
    for file_path in sorted(by_file.keys()):
        print(f'\n📄 {file_path}')
        for issue in by_file[file_path]:
            severity = issue.get('severity', 'UNKNOWN')
            line = issue.get('line', 'N/A')
            msg = issue.get('message', 'No message')
            rule = issue.get('rule', '').split(':')[-1]
            
            severity_icon = '🔴' if severity == 'CRITICAL' else '🟡' if severity == 'MAJOR' else '🔵'
            print(f'  {severity_icon} Line {line}: {msg}')
            print(f'     Rule: {rule}')
except Exception as e:
    print(f'Error parsing JSON: {e}', file=sys.stderr)
"

echo -e "\n${GREEN}View all issues at:${NC}"
echo "https://sonarcloud.io/project/issues?resolved=false&id=${PROJECT_KEY}"
