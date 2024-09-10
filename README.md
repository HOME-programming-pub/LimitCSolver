![Latest Workflow](https://github.com/HOME-programming-pub/LimitCSolver/actions/workflows/dotnet-desktop.yml/badge.svg)

LimitCSolver is a support and learning tool for students who learn the very basics of C programming at Hochschule Merseburg. The main purpose of the tool is to enable a better understanding of the scoping rules in C programs.

Written in C# and .NET using Windows Presentation Foundation, the software runs on Windows only.

Source code is organized as a Visual Studio 2022 Solution including the following projects:

* LimitCInterpreter: an interpreter that can understand a tiny fraction of the C programming language (called LimitC), sufficient to check the very basic scoping rules of the language
* LimitCInterpreter.Test: the interpreter's test cases (not enough to be honest)
* MainApplication: the main UI to load simple C programs and create protocols of the variables states at certain check-points (label comments) and compare with the real values that would occur during program execution
* ProtocolInputApplication: a little helper to create task files that can be loaded by the MainApplication
* LimitCGenerator: a tool that can generate random LimitC programs to be used as tasks for practicing and improve understanding 

The project depends on the following NuGet-packages (see license files too):
* CommunityToolkit.Mvvm (License: MIT)
* Newtonsoft.Json (License: MIT)
* Antlr4.Runtime.Standard (License: BSD 3-Clause)

The code has been written by students of Hochschule Merseburg.

Project contributors:
* Lukas Reinicke, first implementation in his bachelor thesis
* Paul Lüttich, coding of the LimitC-Generator in his master thesis
* Sven Karol, supervision and (some) coding 
