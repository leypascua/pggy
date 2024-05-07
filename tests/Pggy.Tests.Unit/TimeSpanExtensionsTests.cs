using Pggy.Cli.Infrastructure;

namespace Pggy.Tests.Unit
{
    public class TimeSpanExtensionsTests
    {
        [Fact]
        public void Humanize_ReturnsMinutesSeconds_GivenLT120Minutes()
        {
            TimeSpan input = new TimeSpan(0, 70, 13);

            string output = input.Humanize(); 

            Assert.Equal("70 minutes 13 seconds", output);
        }

        [Fact]
        public void Humanize_ReturnsSeconds_GivenLT120Seconds()
        {
            TimeSpan input = new TimeSpan(0, 0, 115);

            string output = input.Humanize();

            Assert.Equal("115 seconds", output);
        }

        [Fact]
        public void Humanize_ReturnsHMS_GivenMT120Minutes()
        {
            TimeSpan input = new TimeSpan(0, 121, 15);

            string output = input.Humanize();

            Assert.Equal("2 hours 1 minute 15 seconds", output);
        }

        [Fact]
        public void Humanize_ReturnsHS_GivenMT120Minutes()
        {
            TimeSpan input = new TimeSpan(0, 120, 15);

            string output = input.Humanize();

            Assert.Equal("2 hours  15 seconds", output);
        }
    }
}