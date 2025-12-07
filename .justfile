# Bumps build version and packs dotnet tool
publish:
  just bump-version
  just pack

# packs dotnet tool without verison change
pack:
	dotnet pack ./MG-CLI/MG-CLI.csproj -c Release
	dotnet tool uninstall --global mg-cli || true
	dotnet tool install --global mg-cli --add-source "./MG-CLI/nupkg" --no-cache

# Bumps the last number in build version
bump-version:
  #!/bin/bash
  set -e
  csproj="MG-CLI/MG-CLI.csproj"
  # Extract current version using awk (macOS compatible)
  version=$(awk -F'[><]' '/<Version>/{print $3; exit}' "$csproj")
  if [ -z "$version" ]; then
    version="1.0.0"
  fi
  IFS='.' read -r major minor patch <<< "$version"
  patch=$((patch + 1))
  new_version="$major.$minor.$patch"
  # Update the version in the csproj
  sed -i '' "s|<Version>$version</Version>|<Version>$new_version</Version>|" "$csproj"
  echo "Bumped version: $version -> $new_version"