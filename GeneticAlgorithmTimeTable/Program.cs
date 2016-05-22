using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using GAF;
using GAF.Operators;

namespace GeneticAlgorithmTimeTable
{
    class Program
    {
        public static Random Ran = new Random();

        private static void Main(string[] args)
        {
            // 시간표 편성 대상 강좌 읽어오기
            StreamReader reader = new StreamReader("CourseInformation.txt", Encoding.Unicode);
            var courses = ReadCourses(reader);
            reader.Close();
            reader = new StreamReader("StudentDemand.txt", Encoding.Unicode);
            var studentDemands = ReadStudentDemands(reader);
            reader.Close();

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
                    
                    // Gene List에 추가
                    chromosome.Genes.Add(new Gene(courseGene));
                }

                // 가능해를 population에 더해준다.
                population.Solutions.Add(chromosome);
            }
            #endregion

            #region 세대 진행 연산 부분

            // 엘리트 연산 - ElitismPercentage
            var elite = new Elite(Constants.Instance.ElitismPercentage);

            // 이점 교차 연산 - CrossoverProbability
            var crossover = new Crossover(Constants.Instance.CrossoverProbability, true, CrossoverType.DoublePoint, ReplacementMethod.GenerationalReplacement);

            // 돌연변이 연산
            var mutate = new CustomMutate(Constants.Instance.MutateProbability);

            // GeneticAlgorithm 생성
            var ga = new GeneticAlgorithm(population, CalculateFitness);

            //hook up to some useful events
            ga.OnGenerationComplete += ga_OnGenerationComplete;
            ga.OnRunComplete += ga_OnRunComplete;

            //add the operators
            ga.Operators.Add(elite);
            ga.Operators.Add(crossover);
            ga.Operators.Add(mutate);

            //run the GA
            ga.Run(Terminate);

            #endregion
        }

        /// <summary>
        /// 교수자 - 강좌인덱스(chromosome에서의 순서) map
        /// </summary>
        static Dictionary<string, List<int>> m_mapCourseByTeacher = new Dictionary<string, List<int>>();

        /// <summary>
        /// 학년 - 강좌인덱스(chromosome에서의 순서) map
        /// </summary>
        static Dictionary<int, List<int>> m_mapCourseByYear = new Dictionary<int, List<int>>();

        /// <summary>
        /// 강좌 ID - 강좌 인덱스 map
        /// </summary>
        static Dictionary<string, List<int>> m_mapCourseByID = new Dictionary<string, List<int>>();

        /// <summary>
        /// 강좌 정보를 읽어와서 리스트로 만듭니다.
        /// </summary>
        private static List<Course> ReadCourses(StreamReader reader)
        {
            var courses = new List<Course>();

            reader.ReadLine();  // 첫줄은 column name이므로 건너뛰기
            while (reader.Peek() >= 0)
            {
                string line = reader.ReadLine();
                courses.Add(new Course(line, '\t'));
            }

            // fixedTime이 있는 강좌를 앞으로 보낸다
            courses.Sort((x, y) => y.FixedTime.CompareTo(x.FixedTime));
           
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

                // 학년-과목 매핑
                List<int> courseIndices2;
                if (m_mapCourseByYear.TryGetValue(courses[i].Year, out courseIndices2))
                {
                    courseIndices2.Add(i);
                }
                else
                {
                    courseIndices2 = new List<int> { i };
                    m_mapCourseByYear.Add(courses[i].Year, courseIndices2);
                }

                // ID-과목 매핑
                List<int> courseIndices3;
                if (m_mapCourseByID.TryGetValue(courses[i].ID, out courseIndices3))
                {
                    courseIndices3.Add(i);
                }
                else
                {
                    courseIndices3 = new List<int> { i };
                    m_mapCourseByID.Add(courses[i].ID, courseIndices3);
                }
            }

            return courses;
        }

        /// <summary>
        /// 학생 수요 정보를 읽어옵니다.
        /// </summary>
        private static List<List<string>> ReadStudentDemands(StreamReader reader)
        {
            var students = new List<List<string>>();
            
            while (reader.Peek() >= 0)
            {
                string line = reader.ReadLine();
                string[] tokens = line.Split('\t');
                List<string> demands = new List<string>();
                for (int i = 2; i < tokens.Length; i+= 2)
                {
                    if (tokens[i].Length > 0)
                        demands.Add(tokens[i].Trim());
                }
                students.Add(demands);
            }

            return students;
        }

        /// <summary>
        /// 특정 해가 강한 제약조건을 만족하는지 여부를 반환한다.
        /// </summary>
        private static bool CheckValidity(Chromosome chromosome)
        {
            // 강한 제약 조건 1. 같은 교수자일 경우 동일 시간을 피한다.
            foreach (List<int> sameTeacherCourses in m_mapCourseByTeacher.Values)
            {
                if (sameTeacherCourses.Count > 1)
                {
                    for (int i = 0; i < sameTeacherCourses.Count; ++i)
                    {
                        CourseGene lhs = (CourseGene)chromosome.Genes[sameTeacherCourses[i]].ObjectValue;
                        for (int j = i + 1; j < sameTeacherCourses.Count; ++j)
                        {
                            CourseGene rhs = (CourseGene)chromosome.Genes[sameTeacherCourses[j]].ObjectValue;
                            if (true == lhs.IsOverlap(rhs))
                                return false;
                        }
                    }
                }
            }

            // 강한 제약 조건 2. 전필 과목에 한해서 같은 학년 수업이고 동일 과목이 아닌 경우 동일 시간을 피한다.
            foreach (List<int> sameYearCourses in m_mapCourseByYear.Values)
            {
                if (sameYearCourses.Count > 1)
                {
                    for (int i = 0; i < sameYearCourses.Count; ++i)
                    {
                        CourseGene lhs = (CourseGene)chromosome.Genes[sameYearCourses[i]].ObjectValue;
                        if (false == lhs.CourseInfo.IsMandatory)
                            continue;
                        for (int j = i + 1; j < sameYearCourses.Count; ++j)
                        {
                            CourseGene rhs = (CourseGene)chromosome.Genes[sameYearCourses[j]].ObjectValue;
                            if (lhs.CourseInfo.ID != rhs.CourseInfo.ID && true == lhs.IsOverlap(rhs))
                                return false;
                        }
                    }
                }
            }

            return true;
        }

        static void ga_OnRunComplete(object sender, GaEventArgs e)
        {
            var fittest = e.Population.GetTop(1)[0];
            foreach (var gene in fittest.Genes)
            {
                Console.WriteLine(((CourseGene)gene.ObjectValue).ToString());
            }
        }

        private static void ga_OnGenerationComplete(object sender, GaEventArgs e)
        {
            var fittest = e.Population.GetTop(1)[0];
            int invalidCount = 0;
            foreach (Chromosome chromosome in e.Population.Solutions)
            {
                if (false == CheckValidity(chromosome))
                    ++invalidCount;
            }

            Console.WriteLine(string.Format("{0}th Generation has {1} average.", e.Generation, e.Population.AverageFitness));
        }

        /// <summary>
        /// 적합도를 계산합니다.
        /// </summary>
        private static double CalculateFitness(Chromosome chromosome)
        {
            if (false == CheckValidity(chromosome))
                return 0;
            else
                return 0.5 + Ran.NextDouble() / 2;
            //var distanceToTravel = CalculateDistance(chromosome);
            //return 1 - distanceToTravel / 10000;
        }

        /// <summary>
        /// 탈출 조건을 계산합니다.
        /// </summary>
        public static bool Terminate(Population population, int currentGeneration, long currentEvaluation)
        {
            return currentGeneration > 400;
        }
    }
}
