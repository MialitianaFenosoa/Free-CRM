namespace Infrastructure.CSVManager;

public class Util
{
    public static T GetRandomStatus<T>(Random random, T[] statuses, int[] weights) where T : Enum
    {
        if (statuses.Length != weights.Length)
            throw new ArgumentException("Statuses and weights must have the same length.");

        int totalWeight = weights.Sum();
        int randomNumber = random.Next(0, totalWeight);

        for (int i = 0; i < statuses.Length; i++)
        {
            if (randomNumber < weights[i])
            {
                return statuses[i];
            }
            randomNumber -= weights[i];
        }

        return statuses.FirstOrDefault(); 
    }
    
    public static string GetRandomValue(List<string> list, Random random)
    {
        return list[random.Next(list.Count)];
    }
}