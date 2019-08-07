﻿using ClaimsTransformation.Language.DOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace ClaimsTransformation.Engine
{
    public class ExpressionVisitor
    {
        public ExpressionVisitor(IClaimsTransformationContext context)
        {
            this.Context = context;
        }

        internal ConditionStates ConditionStates { get; private set; }

        public IClaimsTransformationContext Context { get; private set; }

        public Claim Claim { get; private set; }

        public ClaimProperty Property { get; internal set; }

        public object Visit(Expression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.Literal:
                    return this.Visit(expression as LiteralExpression);
                case ExpressionType.ClaimPropery:
                    return this.Visit(expression as ClaimPropertyExpression);
                case ExpressionType.ConditionProperty:
                    return this.Visit(expression as ConditionPropertyExpression);
                case ExpressionType.Unary:
                    return this.Visit(expression as UnaryExpression);
                case ExpressionType.Binary:
                    return this.Visit(expression as BinaryExpression);
                case ExpressionType.Call:
                    return this.Visit(expression as CallExpression);
                case ExpressionType.Condition:
                    return this.Visit(expression as ConditionExpression);
                case ExpressionType.Issue:
                    return this.Visit(expression as IssueExpression);
                case ExpressionType.Rule:
                    return this.Visit(expression as RuleExpression);
                default:
                    throw new NotImplementedException();
            }
        }

        public object Visit(LiteralExpression expression)
        {
            if (expression == null)
            {
                return null;
            }
            return expression.Value;
        }

        public object Visit(ClaimPropertyExpression expression)
        {
            if (expression == null)
            {
                return null;
            }
            return expression.Name;
        }

        public object Visit(ConditionPropertyExpression expression)
        {
            throw new NotImplementedException();
        }

        public object Visit(UnaryExpression expression)
        {
            throw new NotImplementedException();
        }

        public object Visit(BinaryExpression expression)
        {
            var left = this.Visit(expression.Left);
            var @operator = this.Visit(expression.Operator);
            var right = this.Visit(expression.Right);
            return ExpressionEvaluator.Evaluate(this, left, @operator, right);
        }

        public object Visit(CallExpression expression)
        {
            throw new NotImplementedException();
        }

        public object Visit(ConditionExpression expression)
        {
            var claims = new List<Claim>();
            if (!expression.IsEmpty)
            {
                var identifier = Convert.ToString(this.Visit(expression.Identifier));
                var predicate = this.BuildPredicate(expression.Expressions);
                foreach (var claim in this.Context.Input)
                {
                    if (!predicate(claim))
                    {
                        continue;
                    }
                    claims.Add(claim);
                }
                if (claims.Count > 0)
                {
                    this.ConditionStates[expression].Claims = claims;
                    this.ConditionStates[expression].IsMatch = true;
                }
            }
            else
            {
                this.ConditionStates[expression].Claims = this.Context.Input;
                this.ConditionStates[expression].IsMatch = true;
            }
            return expression;
        }

        public object Visit(IssueExpression expression)
        {
            var issuance = Convert.ToString(this.Visit(expression.Issuance));
            if (this.ConditionStates.IsMatch)
            {
                var claims = new List<Claim>();
                if (this.IsStaticSelector(expression.Expressions))
                {
                    var selector = this.BuildStaticSelector(expression.Expressions);
                    claims.Add(selector());
                }
                else
                {
                    var selector = this.BuildDynamicSelector(expression.Expressions);
                    foreach (var claim in this.ConditionStates.Claims)
                    {
                        claims.Add(selector(claim));
                    }
                }
                this.Context.Output = claims.ToArray();
            }
            return expression;
        }

        public object Visit(RuleExpression expression)
        {
            try
            {
                this.ConditionStates = new ConditionStates();
                foreach (var condition in expression.Conditions)
                {
                    this.Visit(condition);
                }
                this.Visit(expression.Issue);
                return expression;
            }
            finally
            {
                this.ConditionStates = null;
            }
        }

        protected virtual Func<Claim, bool> BuildPredicate(BinaryExpression[] expressions)
        {
            return claim =>
            {
                try
                {
                    this.Claim = claim;
                    foreach (var expression in expressions)
                    {
                        if (!Convert.ToBoolean(this.Visit(expression)))
                        {
                            return false;
                        }
                    }
                    return true;
                }
                finally
                {
                    this.Claim = null;
                }
            };
        }

        protected virtual bool IsStaticSelector(BinaryExpression[] expressions)
        {
            return expressions.All(expression => expression.IsStatic);
        }

        protected virtual Func<Claim> BuildStaticSelector(BinaryExpression[] expressions)
        {
            return () =>
            {
                var properties = new List<ClaimProperty>();
                foreach (var expression in expressions)
                {
                    var property = this.Visit(expression) as ClaimProperty;
                    if (property == null)
                    {
                        throw new NotImplementedException();
                    }
                    properties.Add(property);
                }
                return ClaimFactory.Create(properties);
            };
        }

        protected virtual Func<Claim, Claim> BuildDynamicSelector(BinaryExpression[] expressions)
        {
            return claim =>
            {
                try
                {
                    this.Claim = claim;
                    var properties = new List<ClaimProperty>();
                    foreach (var expression in expressions)
                    {
                        var property = this.Visit(expression) as ClaimProperty;
                        if (property == null)
                        {
                            throw new NotImplementedException();
                        }
                        properties.Add(property);
                    }
                    return ClaimFactory.Create(properties);
                }
                finally
                {
                    this.Claim = null;
                }
            };
        }
    }
}