
using System.IO;

namespace MoneyTrackerApp {
  class Program {
    static void Main() {
      string currentDir = Directory.GetCurrentDirectory();
      string serverBaseFolder = Path.Combine(currentDir, "html", "dist");
      Handlers handlers = new Handlers(serverBaseFolder);
      handlers.StartServer();
    }
  }
}
