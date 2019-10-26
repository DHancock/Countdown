rem @echo off

echo Starting post build batch file: %~nx0

set TargetDir=%~1
set TargetName=%~2
set ProjectDir=%~3
set SolutionDir=%~4

set InputDir=%ProjectDir%Input
set OutputFile=%SolutionDir%Countdown\Resources\wordlist.dat

echo InputDir   = %InputDir%
echo OutputFile = %OutputFile%

start /wait "" "%TargetDir%%TargetName%.exe" "%InputDir%" "%OutputFile%"

echo finished...