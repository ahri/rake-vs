namespace Library
{
    public class Library
    {
        private readonly Dependency dependency;

        public Library(Dependency dependency)
        {
            this.dependency = dependency;
        }

        public void SayHello()
        {
            dependency.Output("Hello World");
        }
    }
}
