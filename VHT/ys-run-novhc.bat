@echo off
start java -jar VHlauncher.jar Rachel
pushd bin\launch-scripts
call run-toolkit-logger.bat
rem call run-toolkit-character-customizer.bat 
call run-toolkit-tts.bat
call run-toolkit-NVBG-C#-all.bat charniak
call run-toolkit-renderer.bat Unity 1280x1024 false -nointro -scene Customizer
popd