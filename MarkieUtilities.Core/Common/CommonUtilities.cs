using System.Reflection;

namespace MarkieUtilities.Core {
    public static class CommonUtilities {
        public static string GetAssemblyLocation() {
            return Assembly.GetExecutingAssembly().Location;
        }
    }
}
