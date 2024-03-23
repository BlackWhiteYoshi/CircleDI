using CircleDI.Defenitions;
using CircleDI.Generation;
using System.Text;

namespace CircleDI.Extensions;

/// <summary>
/// Extension methods on <see cref="StringBuilder"/>.
/// </summary>
public static class StringBuilderExtensions {
    /// <summary>
    /// Appends the given string whereat the first character will be appended as lowercase.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="str"></param>
    public static void AppendFirstLower(this StringBuilder builder, string str) {
        if (str.Length == 0)
            return;

        builder.Append(char.ToLower(str[0]));
        builder.Append(str, 1, str.Length - 1);
    }

    /// <summary>
    /// <para>If implementation is field, it appends <i><see cref="Service.Implementation.Name"/></i>.</para>
    /// <para>Otherwise it appends <i><see cref="Service.Name"/></i>.</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="service"></param>
    public static void AppendServiceField(this StringBuilder builder, Service service) {
        if (service.Implementation.Type == MemberType.Field)
            builder.Append(service.Implementation.Name);
        else {
            builder.Append('_');
            builder.AppendFirstLower(service.Name);
        }
    }

    /// <summary>
    /// <para>If GetAccessor is a property, it appends the <see cref="Service.Name"/>.</para>
    /// <para>Otherwise it appends "Get<i><see cref="Service.Name"/></i>()".</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="service"></param>
    public static void AppendServiceGetter(this StringBuilder builder, Service service) {
        if (service.GetAccessor == GetAccess.Property)
            builder.Append(service.Name);
        else {
            builder.Append("Get");
            builder.Append(service.Name);
            builder.Append("()");
        }
    }


    /// <summary>
    /// Appends the fully qualified name:<br />
    /// <see cref="TypeName.NameSpaceList">NameSpaceList</see>.<see cref="TypeName.ContainingTypeList">ContainingTypeList</see>.<see cref="TypeName.Name">Name</see>&lt;<see cref="TypeName.TypeParameterList">TypeParameterList</see>&gt;
    /// </summary>
    /// <param name="builder"></param>
    public static void AppendFullyQualifiedName(this StringBuilder builder, TypeName type) {
        for (int i = type.NameSpaceList.Count - 1; i >= 0; i--) {
            builder.Append(type.NameSpaceList[i]);
            builder.Append('.');
        }

        for (int i = type.ContainingTypeList.Count - 1; i >= 0; i--) {
            builder.AppendContainingType(type.ContainingTypeList[i]);
            builder.Append('.');
        }

        builder.Append(type.Name);

        if (type.TypeParameterList.Count > 0) {
            builder.Append('<');
            foreach (TypeName? typeParameter in type.TypeParameterList)
                if (typeParameter != null)
                    builder.AppendFullyQualifiedName(typeParameter.Value);
            builder.Append('>');
        }
    }

    /// <summary>
    /// Appdends a string beginning with "namespace" and ending with ';':<br />
    /// "namespace {name1}.{name2}.{name3}.[...].{itemN};"
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="namespaceList"></param>
    public static void AppendNamespace(this StringBuilder builder, List<string> namespaceList) {
        builder.Append("namespace ");
        for (int i = namespaceList.Count - 1; i > 0; i--) {
            builder.Append(namespaceList[i]);
            builder.Append(".");
        }
        builder.Append(namespaceList[0]);
        builder.Append(';');
    }

    public static void AppendContainingType(this StringBuilder builder, ContainingType containingType) {
        builder.Append(containingType.Name);

        if (containingType.TypeParameterList.Count > 0) {
            builder.Append('<');

            builder.Append(containingType.TypeParameterList[0]);
            for (int i = 1; i < containingType.TypeParameterList.Count; i++) {
                builder.Append(", ");
                builder.Append(containingType.TypeParameterList[i]);
            }

            builder.Append('>');
        }
    }
}
