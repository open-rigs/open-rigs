using Bonsai;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Bonsai.Harp;
using SpinnakerNET;

namespace OpenRigs.Video
{
    [Description("Configures and initializes a Spinnaker camera for triggered acquisition.")]
    public class TriggeredSpinnaker : Bonsai.Spinnaker.SpinnakerCapture
    {
        public TriggeredSpinnaker()
        {
            ExposureTime = 1e6 / 50 - 1000;
            Binning = 1;
            PixelFormat = PixelFormatEnums.Mono8.ToString();
        }

        [TypeConverter(typeof(PixelFormatConverter))]
        [Description("The pixel format the camera is set to before acquisition starts. Leave empty to keep the format the camera is already in. The default Mono8 matches the 8-bit mono video writers.")]
        public string PixelFormat { get; set; }

        [Description("The duration of each individual exposure, in microseconds. In general, this should be 1 / frameRate - 1 millisecond to prepare for next trigger.")]
        public double ExposureTime { get; set; }

        [Description("The gain of the sensor.")]
        public double Gain { get; set; }

        [Description("The size of the binning area of the sensor, e.g. a binning size of 2 specifies a 2x2 binning region.")]
        public int Binning { get; set; }

        protected override void Configure(IManagedCamera camera)
        {
            try { camera.AcquisitionStop.Execute(); }
            catch { }
            // The camera keeps the format the last program left (SpinView, Zapit) until a power cycle.
            // Set it before binning because a format change can reset the size limits.
            ConfigurePixelFormat(camera);
            camera.BinningSelector.Value = BinningSelectorEnums.All.ToString();
            camera.BinningHorizontalMode.Value = BinningHorizontalModeEnums.Sum.ToString();
            camera.BinningVerticalMode.Value = BinningVerticalModeEnums.Sum.ToString();
            camera.BinningHorizontal.Value = Binning;
            camera.BinningVertical.Value = Binning;
            camera.AcquisitionFrameRateEnable.Value = false;
            camera.TriggerMode.Value = TriggerModeEnums.On.ToString();
            camera.TriggerSelector.Value = TriggerSelectorEnums.FrameStart.ToString();
            camera.TriggerSource.Value = TriggerSourceEnums.Line0.ToString();
            camera.TriggerOverlap.Value = TriggerOverlapEnums.ReadOut.ToString();
            camera.TriggerActivation.Value = TriggerActivationEnums.RisingEdge.ToString();
            camera.ExposureAuto.Value = ExposureAutoEnums.Off.ToString();
            camera.ExposureMode.Value = ExposureModeEnums.Timed.ToString();
            camera.ExposureTime.Value = ExposureTime;
            camera.DeviceLinkThroughputLimit.Value = camera.DeviceLinkThroughputLimit.Max;
            camera.GainAuto.Value = GainAutoEnums.Off.ToString();
            camera.Gain.Value = Gain;
            base.Configure(camera);
        }

        void ConfigurePixelFormat(IManagedCamera camera)
        {
            var pixelFormat = PixelFormat;
            if (string.IsNullOrEmpty(pixelFormat) || camera.PixelFormat.Value.String == pixelFormat)
            {
                return;
            }

            if (!camera.PixelFormat.IsWritable)
            {
                throw new InvalidOperationException(string.Format(
                    "Camera {0} is in pixel format {1} and its PixelFormat node is not writable. It cannot be set to {2}.",
                    camera.DeviceSerialNumber.Value, camera.PixelFormat.Value.String, pixelFormat));
            }

            try { camera.PixelFormat.Value = pixelFormat; }
            catch (SpinnakerException ex)
            {
                throw new InvalidOperationException(string.Format(
                    "Camera {0} could not be set to pixel format {1}.",
                    camera.DeviceSerialNumber.Value, pixelFormat), ex);
            }
        }

        class PixelFormatConverter : StringConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
            {
                return true;
            }

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return new StandardValuesCollection(Enum.GetNames(typeof(PixelFormatEnums)));
            }
        }

        public IObservable<Timestamped<VideoDataFrame>> Generate<TPayload>(IObservable<Timestamped<TPayload>> source)
        {
            var frames = Generate();
            return frames
                .Select(frame => new VideoDataFrame(frame.Image, frame.ChunkData.FrameID, frame.ChunkData.Timestamp))
                .FillGaps(frame => frame.ChunkData.FrameID, (previous, current) => (int)(current - previous - 1))
                .Zip(source, (frame, payload) => Timestamped.Create(frame, payload.Seconds))
                .Where(timestamped => timestamped.Value != null);
        }
    }
}
