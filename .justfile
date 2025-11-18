pack:
	dotnet pack ./MG-CLI/MG-CLI.csproj -c Release
	dotnet tool uninstall --global mg-cli || true
	dotnet tool install --global mg-cli --add-source "./MG-CLI/nupkg" --no-cache