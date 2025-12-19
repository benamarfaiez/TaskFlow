using AutoFixture;
using AutoFixture.Kernel;
using System.Linq;
using System.Reflection;


namespace FlowTasks.Tests.Common
{
    public class EfCoreCustomization : ICustomization
    {
        public void Customize(IFixture fixture)
        {
            fixture.Behaviors
                .OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(b => fixture.Behaviors.Remove(b));
            fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            // Ignorer toutes les propriétés virtuelles (navigation properties EF)
            fixture.Customizations.Add(new IgnoreVirtualMembersBuilder());
        }
    }

    public class IgnoreVirtualMembersBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (request is PropertyInfo pi && pi.PropertyType.IsClass && pi.GetGetMethod().IsVirtual)
                return new OmitSpecimen();

            return new NoSpecimen();
        }
    }
}
