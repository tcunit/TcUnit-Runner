# Frequently asked questions

Here you'll find the most commonly asked questions and their answers.
If you don’t find what you are looking for here, you can look through the:

- [Open](https://github.com/tcunit/TcUnit-Runner/issues?q=is%3Aopen+is%3Aissue) and [closed](https://github.com/tcunit/TcUnit-Runner/issues?q=is%3Aissue+is%3Aclosed) issues on GitHub
- [Discussions](https://github.com/tcunit/TcUnit/discussions) on GitHub

---

1. [Does TcUnit-Runner only work with Jenkins?](#1-does-tcunit-runner-only-work-with-jenkins)
2. [When launching a build of the Jenkins project, the TcUnit/xUnit results are never returned. Why?](#2-when-launching-a-build-of-the-jenkins-project-the-tcunitxunit-results-are-never-returned-why)
3. [Can I use a virtual machine as Jenkins agent for running TcUnit tests?](#3-can-i-use-a-virtual-machine-as-jenkins-agent-for-running-tcunit-tests)

---

## 1. Does TcUnit-Runner only work with Jenkins?

No, Jenkins is just used as one example of an automation server.
Any automation server of your choice should work fine, as long as it can execute the `LaunchTcUnit.bat` windows batch script or the `TcUnit-Runner.exe` windows executable, and it can handle standard  [xUnit/JUnit XML format](https://llg.cubic.org/docs/junit/) it should work just fine.
Here is an example configuration for a [GitHub action](https://tcunit.org/tcunit-runner-user-manual/#GitHub_action) and one for [Azure pipelines](https://tcunit.org/tcunit-runner-user-manual/#Azure_pipelines).

**Required TcUnit version:** 1.1 or later

## 2. When launching a build of the Jenkins project, the TcUnit/xUnit results are never returned. Why?

Check the console output in Jenkins if it provides any information.
Also, try to build the project manually (by opening Visual Studio) on the build server and check if builds correctly.

## 3. Can I use a virtual machine as Jenkins agent for running TcUnit tests?

Yes, this is possible with the limitation that it is not possible to run TwinCAT on a Windows/shared core but instead it is required that TwinCAT (and more specifically the unit-tests) are to be run on an isolated core in the virtual machine.
Go [here](https://www.sagatowski.com/posts/twincat_and_virtualization/) to read more on the subject.
