﻿using System.Collections.Generic;
using System.Reflection;
using Dahomey.Json.Attributes;
using System.Text.Json.Serialization;
using System.Text.Json;
using System;
using System.Text;

namespace Dahomey.Json.Serialization.Conventions
{
    public class AttributeBasedDiscriminatorConvention<T> : IDiscriminatorConvention
    {
        private readonly JsonSerializerOptions _options;
        private readonly ReadOnlyMemory<byte> _memberName;
        private readonly Dictionary<T, Type> _typesByDiscriminator = new Dictionary<T, Type>();
        private readonly Dictionary<Type, T> _discriminatorsByType = new Dictionary<Type, T>();
        private readonly JsonConverter<T> _jsonConverter;

        public ReadOnlySpan<byte> MemberName => _memberName.Span;

        public AttributeBasedDiscriminatorConvention(JsonSerializerOptions options)
            : this(options, "$type")
        {
        }

        public AttributeBasedDiscriminatorConvention(JsonSerializerOptions options, string memberName)
        {
            _options = options;
            _memberName = Encoding.UTF8.GetBytes(memberName);
            _jsonConverter = options.GetConverter<T>();
        }

        public bool TryRegisterType(Type type)
        {
            JsonDiscriminatorAttribute discriminatorAttribute = type.GetCustomAttribute<JsonDiscriminatorAttribute>();

            if (discriminatorAttribute == null || !(discriminatorAttribute.Discriminator is T discriminator))
            {
                return false;
            }

            _discriminatorsByType[type] = discriminator;
            _typesByDiscriminator.Add(discriminator, type);
            return true;
        }

        public Type ReadDiscriminator(ref Utf8JsonReader reader)
        {
            T discriminator = _jsonConverter.Read(ref reader, typeof(T), _options);
            if (!_typesByDiscriminator.TryGetValue(discriminator, out Type type))
            {
                throw new JsonException($"Unknown type discriminator: {discriminator}");
            }
            return type;
        }

        public void WriteDiscriminator(Utf8JsonWriter writer, Type actualType)
        {
            if (!_discriminatorsByType.TryGetValue(actualType, out T discriminator))
            {
                throw new JsonException($"Unknown discriminator for type: {actualType}");
            }

            _jsonConverter.Write(writer, discriminator, _options);
        }
    }
}