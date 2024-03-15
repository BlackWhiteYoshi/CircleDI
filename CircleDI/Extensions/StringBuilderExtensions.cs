using Microsoft.CodeAnalysis;
using System.Text;

namespace CircleDI;

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
    /// Appends the complete namespace with trailing '.'
    /// </summary>
    /// <param name="namespaceList"></param>
    /// <returns></returns>
    public static void AppendNamespaceList(this StringBuilder builder, List<string> namespaceList) {
        for (int i = namespaceList.Count - 1; i >= 0; i--) {
            builder.Append(namespaceList[i]);
            builder.Append(".");
        }
    }

    /// <summary>
    /// Appends the complete namespace with trailing '.'
    /// </summary>
    /// <param name="namespaceList"></param>
    /// <returns></returns>
    public static void AppendContainingTypeList(this StringBuilder builder, List<(string name, TypeKind type)> containingTypeList) {
        for (int i = containingTypeList.Count - 1; i >= 0; i--) {
            builder.Append(containingTypeList[i].name);
            builder.Append(".");
        }
    }
}
