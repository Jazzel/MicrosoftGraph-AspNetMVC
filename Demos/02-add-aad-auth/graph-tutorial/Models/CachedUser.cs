// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace graph_tutorial.Models
{
    // Simple class to serialize user details
    public class CachedUser
    {
        [Key]
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
    }
}