using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRigs.Tests;

namespace OpenRigs.Logging.Tests
{
    [TestClass]
    public class LogHarpTests
    {
        const string WorkflowFileName = "LogHarpTest.bonsai";
        const string SubjectId = "TestSubject";
        const string SessionId = "001";

        string logRoot;

        [TestInitialize]
        public void TestInitialize()
        {
            // Log outside the repository so that a failed run never leaves files in the working tree.
            logRoot = Path.Combine(Path.GetTempPath(), "OpenRigs.Logging.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(logRoot);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (logRoot == null || !Directory.Exists(logRoot))
            {
                return;
            }

            try
            {
                Directory.Delete(logRoot, recursive: true);
            }
            catch (IOException)
            {
                // Leaving a stray temp directory behind should not fail the run.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        [TestMethod]
        public void LogHarpDevice_SimulatedBehavior_WritesExpectedDirectoryStructure()
        {
            const string LogName = "SimulatedBehavior";
            var result = RunWorkflow(LogName, count: 20);
            var logFiles = GetLogFiles(result);

            foreach (var logFile in logFiles)
            {
                var relativePath = logFile.Substring(logRoot.Length).TrimStart(Path.DirectorySeparatorChar);
                var segments = relativePath.Split(Path.DirectorySeparatorChar);
                Assert.AreEqual(
                    4,
                    segments.Length,
                    $"Expected the log to sit three folders deep below the log root, but found " +
                    $"'{relativePath}'.{result.Describe()}");

                Assert.AreEqual(
                    $"sub-{SubjectId}",
                    segments[0],
                    $"Expected the subject folder to be named sub-<subject>.{result.Describe()}");

                // LogController lays out the session folder as ses-<session>_date-<datetime>.
                var sessionFolder = segments[1];
                var sessionMatch = Regex.Match(
                    sessionFolder,
                    $@"^ses-{Regex.Escape(SessionId)}_date-(\d{{4}}-\d{{2}}-\d{{2}}T\d{{2}}-\d{{2}}-\d{{2}})$");
                Assert.IsTrue(
                    sessionMatch.Success,
                    $"Expected the session folder to match ses-{SessionId}_date-<datetime> but found " +
                    $"'{sessionFolder}'.{result.Describe()}");

                Assert.IsTrue(
                    DateTime.TryParseExact(
                        sessionMatch.Groups[1].Value,
                        "yyyy-MM-ddTHH-mm-ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out _),
                    $"Could not parse the session date from '{sessionFolder}'.{result.Describe()}");

                // LogHarpDevice demultiplexes by register, giving each its own <LogName>_<address> folder.
                var registerFolder = segments[2];
                var registerMatch = Regex.Match(
                    registerFolder,
                    $@"^{Regex.Escape(LogName)}_(\d+)$");
                Assert.IsTrue(
                    registerMatch.Success,
                    $"Expected the register folder to match {LogName}_<address> but found " +
                    $"'{registerFolder}'.{result.Describe()}");

                var address = int.Parse(registerMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                Assert.IsTrue(
                    BehaviorDeviceSimulator.Registers.Any(register => register.Address == address),
                    $"Address {address} in '{registerFolder}' is not a register of the simulated " +
                    $"device.{result.Describe()}");

                StringAssert.Matches(
                    segments[3],
                    new Regex($@"^{Regex.Escape(registerFolder)}.*\.bin$"),
                    $"Expected the data file to sit inside '{registerFolder}' and be prefixed with " +
                    $"it.{result.Describe()}");
            }
        }

        [DataTestMethod]
        [DataRow("SimulatedBehavior", 20)]
        [DataRow("TestHarp", 100)]
        public void LogHarpDevice_SimulatedBehavior_WritesRequestedSampleCountAcrossRegisters(
            string logName,
            int count)
        {
            var result = RunWorkflow(logName, count);
            var logFiles = GetLogFiles(result);

            // Each register is logged separately, so the requested Count is spread across the
            // per-register logs rather than landing in any single one.
            var samplesPerRegister = logFiles.ToDictionary(
                logFile => Path.GetFileName(Path.GetDirectoryName(logFile)),
                HarpBinaryLog.CountMessages);
            var totalSamples = samplesPerRegister.Values.Sum();

            var breakdown = string.Join(
                ", ",
                samplesPerRegister.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}={entry.Value}"));
            Assert.AreEqual(
                count,
                totalSamples,
                $"Expected the samples across all register logs to add up to the requested Count. " +
                $"Found {logFiles.Length} logs: {breakdown}.{result.Describe()}");
        }

        BonsaiWorkflowResult RunWorkflow(string logName, int count)
        {
            return BonsaiWorkflowRunner.Run(WorkflowFileName, new Dictionary<string, string>
            {
                { "SubjectId", SubjectId },
                { "SessionId", SessionId },
                { "Path", logRoot },
                // The workflow externalizes LogHarpDevice's LogName under this display name.
                { "HarpLogName", logName },
                { "Count", count.ToString(CultureInfo.InvariantCulture) }
            });
        }

        string[] GetLogFiles(BonsaiWorkflowResult result)
        {
            var logFiles = Directory.GetFiles(logRoot, "*.bin", SearchOption.AllDirectories);
            Assert.AreNotEqual(
                0,
                logFiles.Length,
                $"Expected the workflow to write at least one Harp log under '{logRoot}'.{result.Describe()}");
            return logFiles;
        }
    }
}
