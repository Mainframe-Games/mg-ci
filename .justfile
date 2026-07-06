# packs dotnet tool without verison change
pack:
	dotnet pack ./MG-CLI/MG-CLI.csproj -c Release
	dotnet tool uninstall --global mg-cli || true
	dotnet tool install --global mg-cli --add-source "./MG-CLI/nupkg" --no-cache

# builds the docfx site, serves it locally, and opens it in the browser
docs-serve:
	docfx docs/docfx.json --serve --open-browser