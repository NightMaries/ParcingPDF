using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using BitMiracle.Docotic.Pdf;
using Newtonsoft.Json;
using OpenQA.Selenium.BiDi.Modules.BrowsingContext;
using OpenQA.Selenium.DevTools.V128.DOMDebugger;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Misc;

class Program
{
    static void Main(string[] args)
    {
        List<Event> allEvents = new List<Event>();
        
        List<EventType> allEventsType = new List<EventType>();
    

        BitMiracle.Docotic.LicenseManager.AddLicenseData("7LZR7-9LLYF-UDPFT-TNNPN-B06W8");

        List<string> CompeteFile = new List<string>();

        using (var pdf = new PdfDocument("Z:/VisualCodeProjects/ParsingPDF/II_chast_EKP_2024_14_11_24_65c6deea36.pdf"))
        {
            // преобразование pdf в текст и удаление надпичей о страница
            
            string formattedText = pdf.GetTextWithFormatting(); // or use pdf.Pages[i].GetTextWithFormatting()
            
            formattedText =  Regex.Replace(formattedText, @"Стр\.\s+\d+\s+из\s+\d+", string.Empty);

            //сохранение текста для дальнейшей работы
            string pathRaw = "Z:/VisualCodeProjects/ParsingPDF/ParsingPDF/rawText.txt";
            
            //файл уже отпарсенный
            string pathFormated = "Z:/VisualCodeProjects/ParsingPDF/ParsingPDF/FormatedText.txt";
            
            string pathCompleted = "Z:/VisualCodeProjects/ParsingPDF/ParsingPDF/CompletedText.txt";

            File.WriteAllText(pathRaw, formattedText);    
           
            //группировка данных по id

            string file = File.ReadAllText(pathRaw);
            string pattern = @"Основной состав\s*(?<Data>[\s\S]+?)(?=Основной состав|\z)";
        
            string pathType = "Z:/VisualCodeProjects/ParsingPDF/ParsingPDF/TypeText.txt";
        
            ParseEventeType(file,allEventsType);
        
            List<string> file2 = new List<string>();
            int idTypeEvent = 1;
            List<Event> events;
            foreach (Match match in Regex.Matches(file, pattern, RegexOptions.IgnoreCase|RegexOptions.Singleline))
            {
                string list = match.Groups["Data"].Value.Trim();

                events = ListEvent(list.Trim(),pathFormated,idTypeEvent,allEvents);
                
                foreach(var evt in events)
                {
                    CompeteFile.Add($"TypeId = {evt.TypeId} \nID: {evt.Id} \n\tName = {evt.Name} \n\tStartDate = {evt.StartDate}\n\t Country = {evt.Country}\n\t"+
                    $"CountPeople = {evt.CountPeople}\n\t Gender = {evt.Gender}\n\t EndDate = {evt.EndDate}\n\t Region = {evt.Region}\n\t City = {evt.City} "+
                    $" \n\tDiscipline = {evt.Discipline} \n\t Program = {evt.Program}");
                }

                file2.Add($"Id = {idTypeEvent}\n\t Header = :{match.Groups["Data"].Value.Trim()}");
                idTypeEvent++;
            }


            File.WriteAllLines(pathType,file2);
            File.WriteAllLines(pathCompleted,CompeteFile);
            System.Console.WriteLine("COMPLETED");
        }
    }
    
    // Извлечение определенных данных из строк по шаблону
    public static string ExtractDetails(string[] details, string pattern)
    {
        foreach (var line in details)
        {
            if (Regex.IsMatch(line, pattern, RegexOptions.IgnoreCase| RegexOptions.IgnorePatternWhitespace| RegexOptions.Multiline))
            {
                return Regex.Match(line, pattern, RegexOptions.IgnoreCase| RegexOptions.Multiline).Value;
            }
        }
        return string.Empty;
    }

    // Извлечение даты из строки
    public static DateTime? ExtractDate(string[] details, int index)
    {
        var datePattern = @"\d{2}\.\d{2}\.\d{4}";
        int count = 0;

        foreach (var line in details)
        {
            if (Regex.IsMatch(line, datePattern))
            {
                if (count == index)
                {
                    return DateTime.Parse(Regex.Match(line, datePattern).Value);
                }
                count++;
            }
        }

        return null;
    }

    public static List<Event> ListEvent(string list,string path, int typeId, List<Event> allEvents)
    {
        string patternEvent = @"
                (?<ID>\d{16})\s*           # Идентификатор
                (?<Details>(?:[^\n]+\n)+?)   # Остальные строки под ID
                (?=(\d{16})|$)               # Заканчивается перед следующим ID или концом файла
                ";
        
        var events = new List<Event>();
        
        foreach (Match match in Regex.Matches(list, patternEvent, RegexOptions.IgnoreCase|RegexOptions.IgnorePatternWhitespace |RegexOptions.Multiline))
        {
            
            var details = match.Groups["Details"].Value.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            //парсинг наименование соревнования(работает, но не доделал с текстом, который на 2-ух строкфх)
            var name =  ExtractDetails(details,@"^[^\d\s]+(?:\s[^\d\s]+)*\S").Trim();

            //парсинг начала периода
            var startDate = ExtractDate(details,0);
            
            //парсинг страны
            var country = ExtractDetails(details, @"(?<=\d{2}\.\d{2}\.\d{4}\s)([\p{L}\s\-]+)(?=\s|,|\n)").Trim();
            
            //пасинг кол-ва участников
            var countPeople = Convert.ToInt16(ExtractDetails(details,@"\d+(?=\s*\D*$)").Trim());
            
            //парсинг гендера(не доделано)
            var gender = ExtractDetails(details,@"(женщины|мужчины|юноши|девушки|юниоры|мальчики|девочки|юниорки)(?:,?\s*(женщины|мужчины|юноши|девушки|юниоры|мальчики|девочки|юниорки))*");
            
            //парсинг окончания периода
            var endDate = ExtractDate(details, 1);
            
            //парсинг отдельно региона
            var region = ExtractDetails(details, @"(?<=\d{2}\.\d{2}\.\d{4}\s)([\p{L}\s\-]+)(?=,)").Trim();
            
            //парсинг отдельно города
            var city = ExtractDetails(details, @"(?<=,\s+(?:г\.\s|Город\s))([\p{L}\s\-]+)").Trim(); 

            var program  = ExtractDetails(details, @"(?:(?<=КЛАСС\s\-)|(?<=КЛАСС\s)[^\n,]+)(?=.*[Дд]исциплина|$)").Trim();
            
            var discyplinе = ExtractDetails(details, @"(?<=[Дд]исциплина\s)[A-Z0-9]+(-[A-Z0-9]+)*").Trim();

            events.Add(new Event
            {
                Id = match.Groups["ID"].Value,
                TypeId = typeId,
                Name = name ,
                StartDate = startDate,
                Country = country,
                CountPeople = countPeople,
                Gender = gender,
                EndDate = endDate,
                Region = region,
                City= city,
                Program = program,
                Discipline = discyplinе
            });
        }
        
        // Вывод результата
        foreach (var evt in events)
        {
            allEvents.Add(evt);
        }
        
        return events;
    }

    public static void ParseEventeType(string file,List<EventType> allEventsType)
    {
            
            string pathEventType = "Z:/VisualCodeProjects/ParsingPDF/ParsingPDF/EventTypeText.txt";
            
            string patternType= @"(?<=\n)(?<Name>.+?)(?=\s+Основной состав)";

            var eventTypes = new List<EventType>();
            
            int idCounter = 1;

            foreach (Match match in Regex.Matches(file, patternType, RegexOptions.Multiline))
            {
                
                if(match.Groups["Name"].Value.Trim() != "" )
                {eventTypes.Add( new EventType
                {   
                    Id = idCounter++,
                    Name = match.Groups["Name"].Value.Trim()

                });
                }

            }

            List<string> eventTypeText = new List<string>();

            foreach(var ln in eventTypes)
            {
                allEventsType.Add(ln); 
                eventTypeText.Add($"Id = {ln.Id}\n\t Name = {ln.Name}");
                System.Console.WriteLine($"Id = {ln.Id}\n\t Name = {ln.Name}");

            }
            
        File.WriteAllLines(pathEventType,eventTypeText);
    }

}
class EventType
{
    public int Id {get; set;}
    public string Name {get; set;}
}

class Event
{
    public string Id { get; set; }
    public string Name {get; set;}
    
    public int TypeId {get; set;}

    public DateTime? StartDate {get; set;}

    public string Country {get; set;}

    public int CountPeople {get; set;}

    public string Gender {get; set; }
    public DateTime? EndDate {get; set;}

    public string Region {get; set;}

    public string City {get; set;}

    public string Program {get; set;}

    public string Discipline {get; set;}
    
}