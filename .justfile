pack:
	dotnet pack ./CLI/CLI.csproj -c Release
	dotnet tool uninstall --global mg || true
	dotnet tool install --global mg --add-source "./CLI/nupkg" --no-cache