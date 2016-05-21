using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GAF;

namespace GeneticAlgorithmTimeTable
{
    class Program
    {
        public static Random Ran = new Random();

        private static void Main(string[] args)
        {
            // 시간표 편성 대상 강좌 읽어오기
            StreamReader reader = new StreamReader("CourceInformation.txt");
            var courses = ReadCources(reader);

            #region 초기 해 생성 부분
            // 초기 해(chromosome) 집합(population)을 수동으로 생성
            var population = new Population();
            
            // population에 들어갈 chromosome생성
            for (var p = 0; p < Constants.Instance.PopulationSize; p++)
            {
                // chromosome은 하나의 해 즉, TimeTable을 의미한다
                var chromosome = new Chromosome();
                
                // chromosome에 모든 코스에 해당하는 gene이 정해진 순서대로 배치되도록 한다.
                foreach (var course in courses)
                {
                    // Gene은 시간이 할당된 수업을 의미한다
                    // 새로운 CourseGene 객체를 생성하면 시간이 랜덤으로 배정된다.
                    CourseGene courseGene = new CourseGene(course);

                    // 필수 제약 조건을 만족하지 않으면 시간 재부여
                    while (false == CheckValidity(chromosome.Genes, courseGene))
                    {
                        courseGene.GenerateRandomTime();
                    }

                    // 필수 제약 조건을 만족했으면 Gene List에 추가
                    chromosome.Genes.Add(new Gene(courseGene));
                }

                // 가능해를 population에 더해준다.
                population.Solutions.Add(chromosome);
            }
            #endregion

            #region 세대 진행 연산 부분
            // 세대 연산 부분

            #endregion
        }

        /// <summary>
        /// 교수자 - 강좌인덱스(chromosome에서의 순서) map
        /// </summary>
        static Dictionary<string, List<int>> m_mapCourseByTeacher = new Dictionary<string, List<int>>();

        /// <summary>
        /// 학년 - 전필강좌인덱스(chromosome에서의 순서) map
        /// </summary>
        static Dictionary<int, List<int>> m_mapMandatoryCourseByYear = new Dictionary<int, List<int>>();

        /// <summary>
        /// 강좌 정보를 읽어와서 리스트로 만듭니다.
        /// </summary>
        private static List<Course> ReadCources(StreamReader reader)
        {
            var courses = new List<Course>();

            while (reader.Peek() >= 0)
            {
                string line = reader.ReadLine();
                courses.Add(new Course(line, '\t'));
            }

            // fixedTime이 있는 강좌를 앞으로 보낸다
            courses.Sort((x, y) => x.FixedTime.CompareTo(y.FixedTime));
           
            // 제약 조건 검사를 위한 매핑 테이블 생성
            for (int i = 0; i < courses.Count; ++i)
            {
                // 교수자-과목 매핑
                List<int> courseIndices1;
                if (m_mapCourseByTeacher.TryGetValue(courses[i].Teacher, out courseIndices1))
                {
                    courseIndices1.Add(i);
                }
                else
                {
                    courseIndices1 = new List<int> { i };
                    m_mapCourseByTeacher.Add(courses[i].Teacher, courseIndices1);
                }

                // 학년-전필과목 매핑
                if (courses[i].IsMandatory)
                {
                    List<int> courseIndices2;
                    if (m_mapMandatoryCourseByYear.TryGetValue(courses[i].Year, out courseIndices2))
                    {
                        courseIndices2.Add(i);
                    }
                    else
                    {
                        courseIndices2 = new List<int> { i };
                        m_mapMandatoryCourseByYear.Add(courses[i].Year, courseIndices2);
                    }
                }
            }

            return courses;
        }

        /// <summary>
        /// 새 Gene(수업)이 필수제약조건을 만족하는지 체크한다.
        /// </summary>
        private static bool CheckValidity(List<Gene> genes, CourseGene newGene)
        {
            // 필수 제약 조건 1. 같은 교수자일 경우 동일 시간을 피한다.
            List<int> sameTeacherCourses = m_mapCourseByTeacher[newGene.CourseInfo.Teacher];
            foreach (int index in sameTeacherCourses)
            {
                if (index < genes.Count)
                {
                    if (false == newGene.IsOverlap((CourseGene)genes[index].ObjectValue))
                        return false;
                }
            }

            // 필수 제약 조건 2. 전필 과목에 한해서 같은 학년 전필이고 동일 과목이 아닌 경우 동일 시간을 피한다.
            if (newGene.CourseInfo.IsMandatory)
            {
                List<int> sameYearCourses = m_mapMandatoryCourseByYear[newGene.CourseInfo.Year];
                foreach (int index in sameYearCourses)
                {
                    if (index < genes.Count)
                    {
                        CourseGene targetGene = (CourseGene)genes[index].ObjectValue;
                        if (newGene.CourseInfo.Name != targetGene.CourseInfo.Name && false == newGene.IsOverlap(targetGene))
                            return false;
                    }
                }
            }

            return false;
        }
    }
}
