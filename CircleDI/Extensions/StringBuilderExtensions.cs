using System.Text;

namespace CircleDI;

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
    /// Appends $"{namspace}." when namspace is not empty, otherwise does nothing.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="namspace"></param>
    public static void AppendNameSpace(this StringBuilder builder, string namspace) {
        if (namspace == string.Empty)
            return;
        
        builder.Append(namspace);
        builder.Append('.');
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
}
