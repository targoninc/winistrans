using System.Windows.Automation;

namespace WinIsTransLibrary;

public static class WindowManager
{
    public static List<string> GetWindowsAndChildren(AutomationElement element, int level)
       {
           List<string> output = new();
       
           // Add the current element's name to the list
           string indentation = new(' ', level * 4);
           output.Add($"{indentation}{element.Current.Name}");
       
           // Find all children
           Condition condition = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
           AutomationElementCollection children = element.FindAll(TreeScope.Children, condition);
       
           foreach (AutomationElement child in children)
           {
               output.AddRange(GetWindowsAndChildren(child, level + 1));
           }
       
           return output;
       }
       
       public static List<string> GetAllWindowsAndTheirChildren()
       {
           List<string> output = new();
           AutomationElement desktop = AutomationElement.RootElement;
       
           if (desktop != null)
           {
               Condition condition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
               AutomationElementCollection allWindows = desktop.FindAll(TreeScope.Children, condition);
       
               foreach (AutomationElement window in allWindows)
               {
                   output.AddRange(GetWindowsAndChildren(window, 0));
               }
           }
       
           return output;
       }
}