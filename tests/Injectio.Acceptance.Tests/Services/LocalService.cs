using Injectio.Attributes;
using Injectio.Tests.Library;

namespace Injectio.Acceptance.Tests.Services;

public interface ILocalService { }

[RegisterSingleton(Registration = RegistrationStrategy.SelfWithInterfaces, Duplicate = DuplicateStrategy.Replace)]
public class LocalService : ILocalService { }

public interface ILocalAttributeService { }

[RegisterSingleton]
public class LocalAttributeService : ILocalAttributeService, IService1 { }

public interface ILocalAttributeNameService { }

[RegisterSingleton<ILocalAttributeNameService, LocalAttributeNameService>]
public class LocalAttributeNameService : ILocalAttributeNameService, ILocalAttributeService { }
