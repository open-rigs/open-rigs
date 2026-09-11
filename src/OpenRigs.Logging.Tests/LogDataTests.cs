using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRigs.Tests;

namespace OpenRigs.Logging.Tests
{
    [TestClass]
    public class LogDataTests
    {
        const string WorkflowFileName = "LogDataTest.bonsai";
        const string SubjectId = "TestSubject";
        const string SessionId = "001";

        static readonly string[] ExpectedHeader = { "Seconds", "Value.X", "Value.Y", "Value.Z" };

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

        // The workflow defaults are LogName "Data" and Count 5, so at least one row must differ from
        // both to prove the values are actually being applied rather than coincidentally matching.
        [DataTestMethod]
        [DataRow("Point3Data", 3)]
        [DataRow("TestData", 5)]
        public void LogData_TimestampedPoint3d_WritesRequestedSamplesToNamedLog(string logName, int count)
        {
            var result = RunWorkflow(logName, count);
            var logFile = GetSingleLogFile(result);

            var logFileName = Path.GetFileName(logFile);
            Assert.IsTrue(
                logFileName.StartsWith(logName, StringComparison.Ordinal),
                $"Expected the log file name '{logFileName}' to be prefixed with the requested " +
                $"LogName '{logName}'.{result.Describe()}");

            var lines = File.ReadAllLines(logFile);
            Assert.AreEqual(
                count + 1,
                lines.Length,
                $"Expected a single header row followed by the requested {count} samples in " +
                $"'{logFile}'.{result.Describe()}");

            CollectionAssert.AreEqual(
                ExpectedHeader,
                lines[0].Split(','),
                $"Unexpected header row in '{logFile}'.{result.Describe()}");

            for (var sample = 0; sample < count; sample++)
            {
                var line = lines[sample + 1];
                var columns = line.Split(',');
                Assert.AreEqual(
                    ExpectedHeader.Length,
                    columns.Length,
                    $"Unexpected column count in sample {sample} ('{line}').{result.Describe()}");

                Assert.IsTrue(
                    double.TryParse(columns[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _),
                    $"Expected '{ExpectedHeader[0]}' in sample {sample} to be a number but found " +
                    $"'{columns[0]}'.{result.Describe()}");

                // The workflow maps the element index onto all three axes of the logged point.
                for (var axis = 1; axis < ExpectedHeader.Length; axis++)
                {
                    Assert.IsTrue(
                        int.TryParse(columns[axis], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value),
                        $"Expected '{ExpectedHeader[axis]}' in sample {sample} to be an integer but found " +
                        $"'{columns[axis]}'.{result.Describe()}");
                    Assert.AreEqual(
                        sample,
                        value,
                        $"Unexpected '{ExpectedHeader[axis]}' in sample {sample}.{result.Describe()}");
                }
            }
        }

        [TestMethod]
        public void LogData_TimestampedPoint3d_WritesExpectedDirectoryStructure()
        {
            const string LogName = "Point3Data";
            var result = RunWorkflow(LogName, count: 3);
            var logFile = GetSingleLogFile(result);

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

            // LogController lays out the session folder as ses-<session>_date-<datetime>, where the
            // datetime is a round-trip UTC timestamp with the time separators replaced.
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
                    out var sessionDate),
                $"Could not parse the session date from '{sessionFolder}'.{result.Describe()}");

            // Guards against the session folder picking up a fixed epoch rather than the run time.
            var age = DateTime.UtcNow - sessionDate;
            Assert.IsTrue(
                age > TimeSpan.FromMinutes(-10) && age < TimeSpan.FromMinutes(10),
                $"Expected the session date '{sessionDate:O}' to be close to the time of the run " +
                $"({DateTime.UtcNow:O}).{result.Describe()}");

            Assert.AreEqual(
                LogName,
                segments[2],
                $"Expected the log subfolder to be named after LogName.{result.Describe()}");

            StringAssert.Matches(
                segments[3],
                new Regex($@"^{Regex.Escape(LogName)}_.+\.csv$"),
                $"Expected the data file to sit inside the '{LogName}' folder and be prefixed with " +
                $"it.{result.Describe()}");
        }

        BonsaiWorkflowResult RunWorkflow(string logName, int count)
        {
            return BonsaiWorkflowRunner.Run(WorkflowFileName, new Dictionary<string, string>
            {
                { "SubjectId", SubjectId },
                { "SessionId", SessionId },
                { "Path", logRoot },
                { "LogName", logName },
                { "Count", count.ToString(CultureInfo.InvariantCulture) }
            });
        }

        string GetSingleLogFile(BonsaiWorkflowResult result)
        {
            // The log file name embeds a timestamp, so search for it rather than reconstructing the path.
            var logFiles = Directory.GetFiles(logRoot, "*.csv", SearchOption.AllDirectories);
            Assert.AreEqual(
                1,
                logFiles.Length,
                $"Expected the workflow to write exactly one CSV log file under '{logRoot}'.{result.Describe()}");
            return logFiles[0];
        }
    }
}
