# TcUnit-Runner - TcUnit integration into Jenkins
Welcome to the documentation of TcUnit-Runner - the program that makes it possible to automate runs of TcUnit unit
tests.

TcUnit-Runner is a piece of software that makes it possible to integrate [TcUnit](https://github.com/tcunit/TcUnit)
(TwinCAT unit tests) in a CI/CD software such as Jenkins.

![TcUnit-Runner basic picture](https://github.com/tcunit/TcUnit-Runner/blob/master/img/TcUnit-Runner_basic.jpg)

With Jenkins and a version control system (such as Git), it's possible to automatically run all unit tests in a TwinCAT
project automatically if a TwinCAT project is changed in the version control system. All test results are automatically
generated and reported in standard [xUnit/JUnit XML format](https://llg.cubic.org/docs/junit/) which Jenkins natively
supports.

**Want to get started?**  
Read the [user manual](https://tcunit.org/tcunit-runner-user-manual/).

**Want to download TcUnit-Runner?**  
Go to the [releases](https://github.com/tcunit/TcUnit-Runner/releases).

If you are completely new to unit testing in general and unit testing in TwinCAT in particular it's recommended
to read the:

1. [Core concepts](https://tcunit.org/unit-testing-concepts/) of unit testing
2. [Introduction user guide to TcUnit](https://tcunit.org/introduction-user-guide/) (the TwinCAT unit testing framework)
3. [TcUnit website](https://tcunit.org/)

**Want to contribute?**

The software is developed using Visual Studio 2013 community edition. You will also need to:
1. Download the [Visual Studio Installer Projects Extension](https://marketplace.visualstudio.com/items?itemName=UnniRavindranathan-MSFT.MicrosoftVisualStudio2013InstallerProjects)
for VS2013. The installation of this extension should be done prior to opening the project.
2. Restore NuGet packages. Go to `TOOLS->NuGet Package Manager->Manage NuGet Packages for Solution`. At the top of the window click the `Restore`-button.