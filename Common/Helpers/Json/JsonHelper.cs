namespace Common.Helpers.Json;

public static class JsonHelper
{
    // Retrieve the value of one specific field from a nested JSON structure using a path string.

    // {
    //   "user": {
    //     "name": "John",
    //     "profile": {
    //       "age": 30,
    //       "scores": [85, 92, 78]
    //     }
    //   },
    //   "counters": [
    //     {
    //       "type": "views",
    //       "count": 150
    //     },
    //     {
    //       "type": "likes", 
    //       "count": 42
    //     }
    //   ]
    // }

    // Example 1
    // string fieldPath = "user/name";
    // Result: "John"

    // Example 2
    // string fieldPath = "user/profile/age";
    // Result: 30

    // string fieldPath = "user/profile/scores/1";
    // Result: 92

    public static int GetNestedPropertyValue(dynamic obj, string fieldPath)
    {
        var parts = fieldPath.Split('/');
        dynamic current = obj;

        foreach (var part in parts)
        {
            // Determines whether a path segment represents an array index or a property name.
            if (int.TryParse(part, out int index))
            {
                // Array index access
                current = current[index];
            }
            else
            {
                // Property access
                current = current[part];
            }
        }

        return (int)current;
    }
}
