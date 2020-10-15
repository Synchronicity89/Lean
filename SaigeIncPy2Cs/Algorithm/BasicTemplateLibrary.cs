
using AddReference = clr.AddReference;

public static class BasicTemplateLibrary {
    
    static BasicTemplateLibrary() {
        AddReference("System");
        AddReference("QuantConnect.Algorithm");
        AddReference("QuantConnect.Common");
    }
    
    // 
    //     To use this library place this at the top:
    //     from BasicTemplateLibrary import BasicTemplateLibrary
    // 
    //     Then instantiate the function:
    //     x = BasicTemplateLibrary()
    //     x.Add(1,2)
    //     
    public class BasicTemplateLibrary {
        
        public virtual object Add(object a, object b) {
            return a + b;
        }
        
        public virtual object Subtract(object a, object b) {
            return a - b;
        }
    }
}
