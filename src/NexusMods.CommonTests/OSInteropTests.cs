using Xunit;
using NexusMods.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using System.Diagnostics;

namespace NexusMods.Common.Tests
{
    public class OSInteropTests
    {
        [Fact()]
        public void UsesShellExecuteOnWindows()
        {
            string url = "foobar://test";
            var mockFactory = new Mock<IProcessFactory>();
            var os = new OSInteropWindows(mockFactory.Object);
            os.OpenUrl(url);
            mockFactory.Verify(f => f.Start(It.IsAny<ProcessStartInfo>()), Times.Once());
            mockFactory.Verify(f => f.Start(It.Is<ProcessStartInfo>(psi => psi.UseShellExecute == true)), Times.Once());
        }

        [Fact()]
        public void UsesXDGOpenOnLinux()
        {
            string url = "foobar://test";
            var mockFactory = new Mock<IProcessFactory>();
            var os = new OSInteropLinux(mockFactory.Object);
            os.OpenUrl(url);
            mockFactory.Verify(f => f.Start("xdg-open", url), Times.Once());
        }

        [Fact()]
        public void UsesOpenOnOSX()
        {
            string url = "foobar://test";
            var mockFactory = new Mock<IProcessFactory>();
            var os = new OSInteropOSX(mockFactory.Object);
            os.OpenUrl(url);
            mockFactory.Verify(f => f.Start("open", url), Times.Once());
        }    }
}