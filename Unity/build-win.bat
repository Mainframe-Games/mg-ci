
set UNITY_VERSION=2021.3.25f1
set EXE="C:\Program Files\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"

%EXE% -quit -batchmode -buildTarget Win64 projectPath "BuildTest" -executeMethod "BuildSystem.BuildScript.BuildPlayer" -settings BuildSettings_Win64 -logFile "win64.log"