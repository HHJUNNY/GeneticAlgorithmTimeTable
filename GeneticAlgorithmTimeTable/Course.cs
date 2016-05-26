using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticAlgorithmTimeTable
{    
    public class Course
    {
        public string ID { private set; get; }              // 강좌 번호
        public int Year { private set; get; }               // 학년
        public string Name { private set; get; }            // 강좌 이름
        public int CourseNumber { private set; get; }       // 동일 강좌 중 강좌 번호
        public bool IsMandatory { private set; get; }       // 전공 필수 과목인지 여부
        public int Credit { private set; get; }             // 학점 수
        public int TheoryCredit { private set; get; }       // 이론 학점 수(나머지는 실험)
        public string Teacher { private set; get; }         // 교수자
        public string FixedTime { private set; get; }       // 고정 시간표일 경우 그 표시. 아닌 경우 String.Empty로 처리

        /// <summary>
        /// 텍스트 정보로부터 Course 클래스 객체를 생성합니다.
        /// </summary>
        public Course(string line, char delimiter)
        {
            FixedTime = String.Empty;

            string[] tokens = line.Split(delimiter);
            int i = 0;

            try
            {
                ID = tokens[i++].Trim();
                Year = Int32.Parse(tokens[i++]);
                Name = tokens[i++];
                CourseNumber = Int32.Parse(tokens[i++]);
                IsMandatory = Boolean.Parse(tokens[i++]);
                Credit = Int32.Parse(tokens[i++]);
                TheoryCredit = Int32.Parse(tokens[i++]);
                Teacher = tokens[i++];
                if (tokens.Length >= i && tokens[i].Length > 0)
                    FixedTime = tokens[i++];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2}\t{3}", ID, Name, CourseNumber, Teacher);
        }
    }
}
