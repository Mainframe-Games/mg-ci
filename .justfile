pack:
	dotnet pack ./CLI/CLI.csproj -c Release
	dotnet tool uninstall --global mg || true
	dotnet tool install --global mg --add-source "./CLI/nupkg" --version 1.0.0 --no-cache