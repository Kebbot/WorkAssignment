using System.Globalization;
using WorkAssignment;

while (true)
{
    Console.WriteLine("Waiting command...");
    string? command = Console.ReadLine();

    if (string.IsNullOrEmpty(command))
        continue;

    string[] commandParam = command.Split(' ');

    if (commandParam[0] == "exit")
    {
        Console.WriteLine("Exiting...");
        break;
    }

    if (commandParam.Length != 12)
    {
        Console.WriteLine(
            "Invalid command. \n" +
            "Usage: --file-log <log_file_path> " +
            "--file-output <output_file_path> " +
            "--address-start <start_ip> " +
            "--address-mask <mask> " +
            "--time-start <start_time> " +
            "--time-end <end_time>"
            );
        continue;
    }

    ProcessParametrs(commandParam);
}


// Метод для обработки аргументов и вывода данных
static void ProcessParametrs(string[] args)
{
    try
    {
        var parametrs = ParseParametrs(args);
        var filteredAddresses = FilterAddresses(
            parametrs.logFile, parametrs.startTime, 
            parametrs.endTime, parametrs.startAdress, parametrs.mask);
        var ipCounts = CountIPAddresses(filteredAddresses);
        ResultsToFile(parametrs.outputFile, ipCounts);
        Console.WriteLine("Analysis completed successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
    }

}

// Метод для парсинга аргументов командной строки
static Parametrs ParseParametrs(string[] param)
{
    var parametrs = new Parametrs();
    for (int i = 0; i < param.Length; i += 2)
    {
        switch (param[i])
        {
            case "--file-log":
                parametrs.logFile = param[i + 1];
                break;
            case "--file-output":
                parametrs.outputFile = param[i + 1];
                break;
            case "--address-start":
                parametrs.startAdress = param[i + 1];
                break;
            case "--address-mask":
                parametrs.mask = int.Parse(param[i + 1]);
                break;
            case "--time-start":
                parametrs.startTime = DateTime.ParseExact(param[i + 1], "dd.MM.yyyy", null);
                break;
            case "--time-end":
                parametrs.endTime = DateTime.ParseExact(param[i + 1], "dd.MM.yyyy", null);
                break;
            default:
                Console.WriteLine($"Unknown argument: {param[i]}");
                break;
        }
    }
    return parametrs;
}

// Метод для фильтрации адресов по времени и диапазону адресов
static IEnumerable<string> FilterAddresses(string logFilePath, DateTime startTime, DateTime endTime, string startAddress, int? mask)
{
    if (string.IsNullOrEmpty(logFilePath))
    {
        throw new ArgumentNullException(nameof(logFilePath), "The path to the log file is null or empty.");
    }

    var filteredAddresses = new List<string>();
    try
    {
        using (var reader = new StreamReader(logFilePath))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(' ');
                var address = parts[0];
                var timeString = string.Join(" ", parts.Skip(1));
                var timeParts = timeString.Split(' ');
                var datePart = timeParts[0];
                var timePart = timeParts[1];
                var fullTimeString = $"{datePart} {timePart}";
                var time = DateTime.ParseExact(fullTimeString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

                if (time >= startTime && time <= endTime && IPInRange(address, startAddress, mask))
                {
                    filteredAddresses.Add(address);
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while reading the log file: {ex.Message}");
    }

    return filteredAddresses;
}


// Метод для проверки, находится ли IP в указанном диапазоне
static bool IPInRange(string ip, string startAddress, int? mask)
{
    if (mask == null)
        return true;

    var ipAddress = System.Net.IPAddress.Parse(ip).GetAddressBytes(); // Получаем байты IP-адреса
    var startIpAddress = System.Net.IPAddress.Parse(startAddress).GetAddressBytes(); // Получаем байты стартового IP-адреса

    byte[] maskBytes = new byte[4]; // Преобразуем маску в байты сетевой части
    for (int i = 0; i < 4; i++)
    {
        if (mask > 8)
        {
            maskBytes[i] = 255;
            mask -= 8;
        }
        else if (mask == 8)
        {
            maskBytes[i] = 255;
            mask = 0;
        }
        else
        {
            maskBytes[i] = (byte)(255 - (Math.Pow(2, 8 - (int)mask) - 1));
            mask = 0;
        }
    }

    // Применяем маску к стартовому IP-адресу
    for (int i = 0; i < startIpAddress.Length; i++)
    {
        startIpAddress[i] = (byte)(startIpAddress[i] & maskBytes[i]);
    }

    // Сравниваем IP-адрес с примененной маской и стартовым IP-адресом
    for (int i = 0; i < ipAddress.Length; i++)
    {
        if (ipAddress[i] != startIpAddress[i])
            return false;
    }

    return true;
}


// Метод для подсчета количества обращений с каждого адреса
static Dictionary<string, int> CountIPAddresses(IEnumerable<string> addresses)
{
    var ipCounts = new Dictionary<string, int>();
    foreach (var address in addresses)
    {
        if (!ipCounts.ContainsKey(address))
            ipCounts[address] = 0;
        ipCounts[address]++;
    }
    return ipCounts;
}

// Метод для записи результатов в файл
static void ResultsToFile(string filePath, Dictionary<string, int> ipCounts)
{
    if (string.IsNullOrEmpty(filePath))
    {
        throw new ArgumentNullException(nameof(filePath), "The path to the output file is null or empty.");
    }

    try
    {
        using (var writer = new StreamWriter(filePath, true))
        {
            foreach (var pair in ipCounts)
            {
                writer.WriteLine($"{pair.Key}: {pair.Value}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while writing to the output file: {ex.Message}");
    }
}