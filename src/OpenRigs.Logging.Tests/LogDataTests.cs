using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
            var result = BonsaiWorkflowRunner.Run(WorkflowFileName, new Dictionary<string, string>
            {
                { "SubjectId", SubjectId },
                { "SessionId", SessionId },
                { "Path", logRoot },
                { "LogName", logName },
                { "Count", count.ToString(CultureInfo.InvariantCulture) }
            });

            // The log file name embeds a timestamp, so search for it rather than reconstructing the path.
            var logFiles = Directory.GetFiles(logRoot, "*.csv", SearchOption.AllDirectories);
            Assert.AreEqual(
                1,
                logFiles.Length,
                $"Expected the workflow to write exactly one CSV log file under '{logRoot}'.{result.Describe()}");

            var logFileName = Path.GetFileName(logFiles[0]);
            Assert.IsTrue(
                logFileName.StartsWith(logName, StringComparison.Ordinal),
                $"Expected the log file name '{logFileName}' to be prefixed with the requested " +
                $"LogName '{logName}'.{result.Describe()}");

            var lines = File.ReadAllLines(logFiles[0]);
            Assert.AreEqual(
                count + 1,
                lines.Length,
                $"Expected a single header row followed by the requested {count} samples in " +
                $"'{logFiles[0]}'.{result.Describe()}");

            CollectionAssert.AreEqual(
                ExpectedHeader,
                lines[0].Split(','),
                $"Unexpected header row in '{logFiles[0]}'.{result.Describe()}");

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
    }
}
